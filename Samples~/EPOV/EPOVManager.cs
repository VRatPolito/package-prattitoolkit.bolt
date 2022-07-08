
/*
 * Custom template by F. Gabriele Pratticò {filippogabriele.prattico@polito.it}
 */

using System;
using System.Collections;
using Bolt;
using PrattiToolkit;
using System.Collections.Generic;
using System.Linq;
using PrattiToolkit.VR;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering;
using Valve.VR;
using Valve.VR.InteractionSystem;
using static PrattiToolkit.BoltExtender;

public enum ScaffoldingMode
{
    TUTORIAL,
    TRAINING,
    ASSESSMENT,
    REPLAY
}

[System.Serializable]
public struct FsmItem
{
    public ScaffoldingMode Mode;
    public StateMachine Flow;
}

public class EPOVManager : UnitySingleton<EPOVManager>
{

    protected EPOVManager() { }

    #region Events

    //[SerializeField] private string BOLT_START_STRING = "StartTraining";
    public BoltEventWrapper _startTrainingEvent;
    public BoltEventWrapper _startTutorialEvent;

    #endregion

    #region Editor Visible

    [SerializeField] private bool _getMetaphorFromConfigFile = true;
    [SerializeField] private Metaphor _metaphor;

    [Header("FSMs")]
    //[SerializeField] private StateMachine _trainingFlow;
    [SerializeField] private ScaffoldingMode _mode = ScaffoldingMode.TRAINING;
    [SerializeField] private List<FsmItem> _flows;

    [SerializeField] private List<GameObject> _proscriptionList;

    public SteamVR_Action_Boolean _skipAction = SteamVR_Actions._default.Skip;
    public SteamVR_Action_Boolean _repeatAction = SteamVR_Actions._default.Repeat;


    #endregion

    #region Private Members and Constants

    private StateMachine _activeFlow;
    private Logger _logger;
    private List<MetaphoreController> _metaphoresInScene;

    #endregion

    #region Properties

    public ScaffoldingMode Mode => _mode;

    public Metaphor Metaphor => _metaphor;

    public StateMachine ActiveFlow => _activeFlow;

    public VoiceoverManager VoiceoverController { get; set; }

    public MetaphoreController MetaphorPractical { get; protected set; }

    public Logger Logger => _logger;

    #endregion

    #region MonoBehaviour

    private void Awake()
    {
        if (_getMetaphorFromConfigFile)
        {
            _metaphor = ConfigurationLookUp.Instance.GetEnum("Metaphor", Metaphor);
            _mode = ConfigurationLookUp.Instance.GetEnum("Mode", Mode);
            if (ConfigurationLookUp.Instance.GetBool("ForceJOY", false))
            {
                var jm = FindObjectOfType<JoystickMovement>();
                if (jm != null) jm.enabled = true;
            }
        }

        InitFlows();

        _logger = this.GetComponent<Logger>();

        Assert.IsNotNull(_logger);
        VoiceoverController = this.GetComponentInChildren<VoiceoverManager>();
        Assert.IsNotNull(VoiceoverController);
        VoiceoverController.CanSkip = false;
        VoiceoverController.CanRepeat = true;

        if (Mode == ScaffoldingMode.REPLAY) _metaphor = Metaphor.DUMMY;

        InitMetaphores();

        InitBewEvents();
    }

    private void Start()
    {
        SteamVR_Fade.View(Color.clear, 1);
        StartCoroutine(WaitForLoggerRecording_CR());
    }

    #endregion

    #region Public Methods

    public void DeactivateListed()
    {
        _proscriptionList.ForEach(go=>go.SetActive(false));
        if (MetaphorPractical != null) MetaphorPractical.gameObject.SetActive(false);
    }

    public void ActivateListed()
    {
        if (MetaphorPractical != null) MetaphorPractical.gameObject.SetActive(false);
        _proscriptionList.ForEach(go => go.SetActive(true));
    }

    public void LockPlayer()
    {
        var armswinger = FindObjectOfType<ArmSwinger>();
        if(armswinger!=null)
            armswinger.armSwingingPaused = true;
        if (Valve.VR.InteractionSystem.Player.instance.leftHand != null)
            Valve.VR.InteractionSystem.Player.instance.leftHand.Hide();
        if (Valve.VR.InteractionSystem.Player.instance.rightHand != null)
            Valve.VR.InteractionSystem.Player.instance.rightHand.Hide();
    }

    public void UnLockPlayer()
    {
        var armswinger = FindObjectOfType<ArmSwinger>();
        if (armswinger != null)
            armswinger.armSwingingPaused = false;
        if (Valve.VR.InteractionSystem.Player.instance.leftHand != null)
            Valve.VR.InteractionSystem.Player.instance.leftHand.Show();
        if (Valve.VR.InteractionSystem.Player.instance.rightHand != null)
            Valve.VR.InteractionSystem.Player.instance.rightHand.Show();
    }

    public void FadeInOut(bool toOrFromFade, float duration = 1)
    {
        SteamVR_Fade.View(toOrFromFade?Color.black:Color.clear, duration);
    }

    public void RepositionPlayer(Vector3 worldPos)
    {
        worldPos -= Player.instance.hmdTransform.position ;
        worldPos.y = Player.instance.trackingOriginTransform.position.y;
        Player.instance.trackingOriginTransform.position += worldPos;

    }

    public bool SkipPressed()
    {
        if (_skipAction == null)
            return false;

        return _skipAction.GetStateDown(SteamVR_Input_Sources.LeftHand)
            || _skipAction.GetStateDown(SteamVR_Input_Sources.RightHand);
    }

    public bool RepeatPressed()
    {
        if (_repeatAction == null)
            return false;

        return _repeatAction.GetStateDown(SteamVR_Input_Sources.LeftHand)
               || _repeatAction.GetStateDown(SteamVR_Input_Sources.RightHand);
    }

    public void WaitForUserReady()
    {
        StartCoroutine(WaitForUserReady_CR());
    }

    public void Reload()
    {
        InitMetaphores();

        InitFlows();

        _startTrainingEvent.UnregisterAll();
        _startTutorialEvent.UnregisterAll();

        InitBewEvents();
    }

    #endregion

    #region Helper Methods

    private void InitFlows()
    {
        //_activeFlow = _trainingFlow;
        foreach (var fsmItem in _flows)
        {
            if (fsmItem.Mode == Mode)
                _activeFlow = fsmItem.Flow;

            fsmItem.Flow.enabled = fsmItem.Mode == Mode;
            fsmItem.Flow.gameObject.SetActive(fsmItem.Mode == Mode);
        }

        Assert.IsNotNull(ActiveFlow);
    }

    private void InitMetaphores()
    {
        _metaphoresInScene = FindObjectsOfType<MetaphoreController>().ToList();
        _metaphoresInScene.ForEach(m =>
        {
            if (Mode == ScaffoldingMode.ASSESSMENT)
            {
                DestroyImmediate(m.gameObject);
            }
            else if (m.CurrentMetaphor != Metaphor)
            {
                m.gameObject.SetActive(false);
            }
            else
            {
                MetaphorPractical = m;
            }
        });
    }

    private void InitBewEvents()
    {
        _startTrainingEvent.Graph = ActiveFlow;
        _startTrainingEvent.Logger = Logger;
        _startTrainingEvent.RegisterBoltEvent();

        _startTutorialEvent.Graph = ActiveFlow;
        _startTutorialEvent.Logger = Logger;
        _startTutorialEvent.RegisterBoltEvent();
    }

    #endregion

    #region Events Callbacks

    #endregion

    #region Coroutines

    private IEnumerator WaitForLoggerRecording_CR()
    {
        yield return new WaitUntil(() => _logger.IsReadyAndRecording);
        yield return new WaitUntil(SkipPressed);
        //_startTrainingEvent.Invoke();
        _startTutorialEvent.Invoke();
    }

    private IEnumerator WaitForUserReady_CR()
    {
        yield return new WaitUntil(SkipPressed);
        VoiceoverController.CanSkip = ConfigurationLookUp.Instance.GetBool("Skippable", false);
        if(VoiceoverController.CanSkip)
            VoiceoverController.CanRepeat = false;
        _startTrainingEvent.Invoke();
    }

    #endregion
}
