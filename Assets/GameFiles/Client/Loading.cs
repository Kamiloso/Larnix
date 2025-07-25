using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Larnix.Client
{
    public class Loading : MonoBehaviour
    {
        private const float LOADING_TIME_TEMP = 1f; // seconds

        private bool waiting = false; // trying to end
        private uint FixFrame = 0; // frame at which waiting will start

        private void Awake()
        {
            References.Loading = this;
        }

        private void Update()
        {
            if(waiting)
            {
                if (ReadyEntities() && ReadyChunks())
                    EndLoading();
            }
        }

        public void StartLoading(string info)
        {
            References.LoadingScreen.Enable(info);
        }

        public void StartWaitingFrom(uint fixFrame)
        {
            waiting = true;
            FixFrame = fixFrame;
        }

        private void EndLoading()
        {
            waiting = false;
            References.LoadingScreen.Disable();
            References.MainPlayer.SetAlive();
        }

        private bool ReadyEntities()
        {
            var Ep = References.EntityProjections;
            return (
                Ep.ReceivedSomething &&
                (int)(Ep.LastKnown - FixFrame) > 0 &&
                Ep.GetDelayedEntities() == 0
                );
        }

        private bool ReadyChunks()
        {
            return true; // temporarily
        }
    }
}
