using System.Collections;
using System.Collections.Generic;
using Larnix.Client.Entities;
using Larnix.Client.Terrain;
using UnityEngine;
using Larnix.Core;

namespace Larnix.Client.UI
{
    public class Loading : MonoBehaviour
    {
        private LoadingScreen LoadingScreen => GlobRef.Get<LoadingScreen>();
        private MainPlayer MainPlayer => GlobRef.Get<MainPlayer>();
        private EntityProjections EntityProjections => GlobRef.Get<EntityProjections>();
        private GridManager GridManager => GlobRef.Get<GridManager>();

        private bool _tryingToEnd = false;
        private uint _fixFrame = 0; // frame at which waiting will start

        private void Awake()
        {
            GlobRef.Set(this);
        }

        private void Update()
        {
            if(_tryingToEnd)
            {
                if (EntityProjections.EverythingLoaded(_fixFrame) &&
                    GridManager.LoadedAroundPlayer())
                {
                    EndLoading();
                }
            }
        }

        public void StartLoading(string info)
        {
            LoadingScreen.Enable(info);
        }

        public void StartWaitingFrom(uint fixFrame)
        {
            _tryingToEnd = true;
            _fixFrame = fixFrame;
        }

        private void EndLoading()
        {
            _tryingToEnd = false;
            LoadingScreen.Disable();
            MainPlayer.SetAlive();
        }
    }
}
