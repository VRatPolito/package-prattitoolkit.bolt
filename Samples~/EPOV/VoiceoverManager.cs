
/*
 * Custom template by F. Gabriele Pratticò {filippogabriele.prattico@polito.it}
 */

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using Valve.VR;
using static PrattiToolkit.BoltExtender;

[RequireComponent(typeof(AudioClip))]
[RequireComponent(typeof(AudioSource))]
public class VoiceoverManager : MonoBehaviour
{
	#region Events
		
	#endregion
	
	#region Editor Visible

    public BoltEventWrapper OnClipOver;
    public BoltEventWrapper OnRepeatRequest;

    #endregion

    #region Private Members and Constants

    private AudioClip _audioClip;
    private AudioSource[] _audioSource;
    private Coroutine _waitNoAudioCoroutine;

    #endregion

    #region Properties

    public bool CanSkip { get; set; }
    public bool CanRepeat { get; set; }

    #endregion

    #region MonoBehaviour

    private void Awake()
    {
        _audioSource = GetComponents<AudioSource>();
        Assert.IsNotNull(_audioSource);
    }

    private void Start()
    {
        OnClipOver.Graph = EPOVManager.Instance.ActiveFlow;
        OnClipOver.Logger = EPOVManager.Instance.Logger;
        OnClipOver.RegisterBoltEvent();

        OnRepeatRequest.Graph = EPOVManager.Instance.ActiveFlow;
        OnRepeatRequest.Logger = EPOVManager.Instance.Logger;
        OnRepeatRequest.RegisterBoltEvent();
    }

    private void Update()
    {
        if (CanSkip &&
            EPOVManager.Instance.Mode == ScaffoldingMode.TRAINING &&
            EPOVManager.Instance.SkipPressed())
        {
            Skip();
        }
        if (CanRepeat &&
            EPOVManager.Instance.Mode == ScaffoldingMode.TRAINING &&
            EPOVManager.Instance.RepeatPressed())
        {
            Repeat();
        }
    }

    #endregion

    #region Public Methods

    public void LoadAndPlay(AudioClip audio, bool sfx = false)
    {
        if (audio == null) return;

        if (sfx)
            _audioSource[1].clip = audio;
        else
        {
            _audioSource[0].Stop();
            _audioSource[0].clip = _audioClip = audio;
        }

        Play(sfx);

    }

    public void Skip()
    {
        if(_audioSource[0] != null) _audioSource[0].Stop();
    }

    public void Repeat()
    {
        _audioSource[0].Stop();
        OnRepeatRequest.Invoke();
        _audioSource[0].Play();
    }

    #endregion

    #region Helper Methods

    private void Play(bool sfx = false)
    {
        if (_waitNoAudioCoroutine != null)
            StopCoroutine(_waitNoAudioCoroutine);

        if(sfx)
        {
            _audioSource[1].Play();
        }
        else
        {
            _audioSource[0].Play();
            _waitNoAudioCoroutine = StartCoroutine(WaitForSound());
        }
    }

    #endregion

    #region Events Callbacks

    #endregion

    #region Coroutines

    public IEnumerator WaitForSound()
    {
        yield return new WaitUntil(() => _audioSource[0].isPlaying == false); 
        OnClipOver.Invoke();
    }

    #endregion

}
