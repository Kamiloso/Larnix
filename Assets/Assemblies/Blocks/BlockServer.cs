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
        public readonly BlockData1 BlockData; // connected to block-saving system

        private IWorldAPI _worldAPI = null;
        public IWorldAPI WorldAPI
        {
            get => _worldAPI ?? throw new InvalidOperationException("Trying to use an uninitialized WorldAPI.");
        }

        private bool _markedToUpdate = false;

        public event EventHandler PreFrameEvent;
        public event EventHandler FrameEventRandom;
        public event EventHandler FrameEventSequential;

        public BlockServer(Vec2Int position, BlockData1 blockData, bool isFront)
        {
            Position = position;
            BlockData = blockData; // should consume a given object
            IsFront = isFront;
        }

        public void InitializeWorldAPI(IWorldAPI worldAPI)
        {
            if (_worldAPI != null)
                throw new InvalidOperationException("WorldAPI already initialized.");

            _worldAPI = worldAPI;
        }

        public void PreFrameTrigger()
        {
            _markedToUpdate = true;
            PreFrameEvent?.Invoke(this, EventArgs.Empty);
        }

        public void FrameUpdateRandom()
        {
            if (_markedToUpdate)
                FrameEventRandom?.Invoke(this, EventArgs.Empty);
        }

        public void FrameUpdateSequential()
        {
            if (_markedToUpdate)
                FrameEventSequential?.Invoke(this, EventArgs.Empty);
            
            _markedToUpdate = false;
        }
    }
}
