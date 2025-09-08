using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Larnix.Blocks;
using System;

namespace Larnix.Blocks
{
    public class BlockServer
    {
        public readonly Vector2Int Position;
        public readonly bool IsFront;
        public BlockData1 BlockData;

        public IWorldAPI WorldAPI { get => _WorldAPI ?? throw new InvalidOperationException("Trying to use an uninitialized WorldAPI."); }
        private IWorldAPI _WorldAPI = null;
        private bool MarkedToUpdate = false;

        public event EventHandler PreFrameEvent;
        public event EventHandler FrameEventRandom;
        public event EventHandler FrameEventSequential;

        public BlockServer(Vector2Int position, BlockData1 blockData, bool isFront)
        {
            Position = position;
            BlockData = blockData;
            IsFront = isFront;
        }

        public void InitializeWorldAPI(IWorldAPI worldAPI)
        {
            if (_WorldAPI != null)
                throw new InvalidOperationException("WorldAPI already initialized.");

            _WorldAPI = worldAPI;
        }

        public void PreFrameTrigger()
        {
            MarkedToUpdate = true;
            PreFrameEvent?.Invoke(this, EventArgs.Empty);
        }

        public void FrameUpdateRandom()
        {
            if (MarkedToUpdate)
            {
                FrameEventRandom?.Invoke(this, EventArgs.Empty);
            }
        }

        public void FrameUpdateSequential()
        {
            if (MarkedToUpdate)
            {
                FrameEventSequential?.Invoke(this, EventArgs.Empty);
            }
            MarkedToUpdate = false;
        }
    }
}
