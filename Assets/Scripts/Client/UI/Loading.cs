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
        private uint _fixFrame = 0; // frame at which waiting has started

        private void Awake()
        {
            GlobRef.Set(this);
        }

        private void Update()
        {
            if (_tryingToEnd)
            {
                bool entitiesLoaded = EntityProjections.EverythingLoaded(_fixFrame);
                bool terrainLoaded = GridManager.LoadedAroundPlayer();

                if (entitiesLoaded && terrainLoaded)
                {
                    EndLoading();
                }
            }
        }

        public void StartLoading(string info)
        {
            if (!MainPlayer.Alive)
            {
                LoadingScreen.Enable(info);
            }
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
            MainPlayer.Alive = true;
        }
    }
}
