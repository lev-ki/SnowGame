using System;
using NerdingRoom.Scripts.Core.NRHandles;
using NerdingRoom.Scripts.Core.NRStats;
using UnityEngine;

using Sessions = NerdingRoom.Scripts.Core.NRSessions;

namespace NerdingRoom.Scripts.SnowGame
{
    public class SnowSessionVisualizer : MonoBehaviour
    {
        public Sessions.NRSessionManager SessionManager;

        public Vector3 EulerAngles_Start;
        public Vector3 EulerAngles_End;
        
        public float NormalizedProgress = 0f;
        
        private NRStat Stat_SessionDuration;
        private NRStat Stat_SessionProgress;
        
        private void Start()
        {
            Stat_SessionDuration = NRStatSystem.Instance.GetStat(Sessions.Handles.Session_TotalDuration.Get(), Sessions.Handles.Entity_SessionManager.Get());
            Stat_SessionProgress = NRStatSystem.Instance.GetStat(Sessions.Handles.Session_Progress.Get(), Sessions.Handles.Entity_SessionManager.Get());

            if (Stat_SessionProgress != null)
            {
                Stat_SessionProgress.OnValueUpdated.AddListener(OnSessionProgressUpdated);
            }
            
            if (SessionManager)
            {
                SessionManager.OnSessionStateEvent.AddListener(OnSessionStateChanged);
            }
        }

        private void OnSessionProgressUpdated()
        {
            NormalizedProgress = (float)(Stat_SessionProgress.GetCalculatedValue() / Stat_SessionDuration.GetCalculatedValue());
            var rotation = transform.rotation;
            rotation.eulerAngles = Vector3.Lerp(EulerAngles_Start, EulerAngles_End, NormalizedProgress);
            transform.rotation = rotation;
        }

        private void OnSessionStateChanged(Sessions.ENRSessionState state)
        {
            
        }
    }
}