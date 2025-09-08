using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Larnix.Client.UI
{
    public class Loading : MonoBehaviour
    {
        private const float LOADING_TIME_TEMP = 1f; // seconds

        private bool waiting = false; // trying to end
        private uint FixFrame = 0; // frame at which waiting will start

        private void Awake()
        {
            Ref.Loading = this;
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
            Ref.LoadingScreen.Enable(info);
        }

        public void StartWaitingFrom(uint fixFrame)
        {
            waiting = true;
            FixFrame = fixFrame;
        }

        private void EndLoading()
        {
            waiting = false;
            Ref.LoadingScreen.Disable();
            Ref.MainPlayer.SetAlive();
        }

        private bool ReadyEntities()
        {
            return Ref.EntityProjections.EverythingLoaded(FixFrame);
        }

        private bool ReadyChunks()
        {
            return Ref.GridManager.LoadedAroundPlayer();
        }
    }
}
