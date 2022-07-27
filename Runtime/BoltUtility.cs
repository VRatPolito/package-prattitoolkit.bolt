/* File Utility C# implementation of class Utility */

#if UNITY_2021_3_OR_NEWER
using Unity.VisualScripting;
#else
using Bolt;
#endif
using UnityEngine.Events;
using Component = UnityEngine.Component;
using Debug = UnityEngine.Debug;
using Math = System.Math;

// global declaration end
namespace VRatPolito.PrattiToolkit.Bolt
{
    public static class BoltExtender
    {
        public const string BEW_PREFIX = "BEW_OCCURED";
        public const string RM_PREFIX = "ROBOT_MOVED";

        [System.Serializable]
        public struct BoltEventWrapper
        {
            public UnityEvent Event;
            public StateMachine Graph;
            public string BOLT_EVT_NAME;

            public ILoggerBEW Logger;

            public void Invoke()
            {
                Event.Invoke();
                if (Logger != null) Logger.LogBewInvoked(this);
            }
        }

        public static void UnregisterAll(this BoltEventWrapper bew)
        {
            bew.Event.RemoveAllListeners();
        }

        public static void RegisterBoltEvent(this BoltEventWrapper bew)
        {
            bew.Event.RegisterBoltEvent(bew.Graph, bew.BOLT_EVT_NAME);
        }

        public static void RegisterBoltEvent(this UnityEvent ue, StateMachine graphStateMachine, string eventName)
        {
            ue.AddListener(() =>
            {
                if(graphStateMachine!=null)
                    graphStateMachine.TriggerUnityEvent(eventName);
            });
        }
    }  
}