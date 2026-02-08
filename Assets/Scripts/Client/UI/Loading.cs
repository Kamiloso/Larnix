using System.Collections;
using System.Collections.Generic;
using Larnix.Client.Entities;
using Larnix.Client.Terrain;
using UnityEngine;

namespace Larnix.Client.UI
{
    public class Loading : MonoBehaviour
    {
        private LoadingScreen LoadingScreen => Ref.LoadingScreen;
        private MainPlayer MainPlayer => Ref.MainPlayer;
        private EntityProjections EntityProjections => Ref.EntityProjections;
        private GridManager GridManager => Ref.GridManager;

        private bool _tryingToEnd = false;
        private uint _fixFrame = 0; // frame at which waiting will start

        private void Awake()
        {
            Ref.Loading = this;
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
