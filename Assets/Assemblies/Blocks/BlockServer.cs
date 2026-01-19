using System.Collections;
using System.Collections.Generic;
using Larnix.Blocks;
using System;
using Larnix.Blocks.Structs;
using Larnix.Core.Vectors;

namespace Larnix.Blocks
{
    public class BlockServer
    {
        public readonly Vec2Int Position;
        public readonly bool IsFront;
        public BlockData1 BlockData;

        public IWorldAPI WorldAPI { get => _WorldAPI ?? throw new InvalidOperationException("Trying to use an uninitialized WorldAPI."); }
        private IWorldAPI _WorldAPI = null;
        private bool MarkedToUpdate = false;

        public event EventHandler PreFrameEvent;
        public event EventHandler FrameEventRandom;
        public event EventHandler FrameEventSequential;

        public BlockServer(Vec2Int position, BlockData1 blockData, bool isFront)
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
