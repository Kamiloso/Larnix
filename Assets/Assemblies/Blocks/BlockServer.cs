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

        public void ReflagForEvents()
        {
            _markedToUpdate = true;
        }

#region Frame Events

        public event EventHandler PreFrameEvent;
        public event EventHandler PreFrameEventSelfMutations;
        public event EventHandler FrameEventConway;
        public event EventHandler FrameEventSequential;
        public event EventHandler FrameEventRandom;
        public event EventHandler FrameEventElectricPropagation;
        public event EventHandler FrameEventElectricFinalize;
        public event EventHandler FrameEventSequentialLate;
        public event EventHandler PostFrameEvent;

        public void PreFrameTrigger() // START 1
        {
            _markedToUpdate = true;
            PreFrameEvent?.Invoke(this, EventArgs.Empty);
        }

        public void PreFrameTriggerSelfMutations() // START 2
        {
            if (_markedToUpdate)
                PreFrameEventSelfMutations?.Invoke(this, EventArgs.Empty);
        }

        public void FrameUpdateConway() // 1
        {
            if (_markedToUpdate)
                FrameEventConway?.Invoke(this, EventArgs.Empty);
        }

        public void FrameUpdateSequential() // 2
        {
            if (_markedToUpdate)
                FrameEventSequential?.Invoke(this, EventArgs.Empty);
        }

        public void FrameUpdateRandom() // 3
        {
            if (_markedToUpdate)
                FrameEventRandom?.Invoke(this, EventArgs.Empty);
        }

        public void FrameUpdateElectricPropagation() // 4
        {
            if (_markedToUpdate)
                FrameEventElectricPropagation?.Invoke(this, EventArgs.Empty);
        }

        public void FrameUpdateElectricFinalize() // 5
        {
            if (_markedToUpdate)
                FrameEventElectricFinalize?.Invoke(this, EventArgs.Empty);
        }

        public void FrameUpdateSequentialLate() // 6
        {
            if (_markedToUpdate)
                FrameEventSequentialLate?.Invoke(this, EventArgs.Empty);
        }

        public void PostFrameTrigger() // END
        {
            if (_markedToUpdate)
                PostFrameEvent?.Invoke(this, EventArgs.Empty);
            
            _markedToUpdate = false;
        }

#endregion

    }
}
