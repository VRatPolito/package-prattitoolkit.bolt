
/*
 * Custom template by F. Gabriele Pratticò {filippogabriele.prattico@polito.it}
 */
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Timeline;
using UnityEngine.Video;
using static PrattiToolkit.TransformExtender;

[CreateAssetMenu(fileName = "ScaffoldingChunk", menuName = "Scaffolding/Chunk", order = 1)]
public class ScaffoldingChunk_SO : ScriptableObject
{
	#region Data

    public AudioClip VoiceOver;

    [Header("PiP")]
    public VideoClip Video;

    [Header("Frustum")]
    public TransformData FrustumTr;
    public Vector3 EndGazePos;

    [Header("Poser")] 
    public AnimationClip AnimationPose;

    #endregion


}
