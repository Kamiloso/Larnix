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

        public BlockServer(Vec2Int position, BlockData1 blockData, bool isFront)
        {
            Position = position;
            BlockData = blockData; // should consume a given object
            IsFront = isFront;
        }

        private IWorldAPI _worldAPI = null;
        public IWorldAPI WorldAPI
        {
            set => _worldAPI = _worldAPI == null ? value : throw new InvalidOperationException("WorldAPI already initialized.");
            get => _worldAPI ?? throw new InvalidOperationException("Trying to use an uninitialized WorldAPI.");
        }

        /// <summary>
        /// Determines whether block is active for event in the current frame.
        /// Resets to false when ID or Variant is changed. Can be prevented
        /// by replacing block with BreakMode = Weak.
        /// </summary>
        public bool EventFlag { get; set; }
        private readonly Dictionary<BlockEvent, EventHandler> _eventHandlers = new();

        public void Subscribe(BlockEvent type, EventHandler handler)
        {
            if (_eventHandlers.TryGetValue(type, out var existing))
                _eventHandlers[type] = existing + handler;
            else
                _eventHandlers[type] = handler;
        }

        public void InvokeEvent(BlockEvent type)
        {
            if (EventFlag)
            {
                if (_eventHandlers.TryGetValue(type, out var handler))
                    handler?.Invoke(this, EventArgs.Empty);
            }
        }

/*#region Frame Events

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
            EventFlag = true;
            PreFrameEvent?.Invoke(this, EventArgs.Empty);
        }

        public void PreFrameTriggerSelfMutations() // START 2
        {
            if (EventFlag)
                PreFrameEventSelfMutations?.Invoke(this, EventArgs.Empty);
        }

        public void FrameUpdateConway() // 1
        {
            if (EventFlag)
                FrameEventConway?.Invoke(this, EventArgs.Empty);
        }

        public void FrameUpdateSequential() // 2
        {
            if (EventFlag)
                FrameEventSequential?.Invoke(this, EventArgs.Empty);
        }

        public void FrameUpdateRandom() // 3
        {
            if (EventFlag)
                FrameEventRandom?.Invoke(this, EventArgs.Empty);
        }

        public void FrameUpdateElectricPropagation() // 4
        {
            if (EventFlag)
                FrameEventElectricPropagation?.Invoke(this, EventArgs.Empty);
        }

        public void FrameUpdateElectricFinalize() // 5
        {
            if (EventFlag)
                FrameEventElectricFinalize?.Invoke(this, EventArgs.Empty);
        }

        public void FrameUpdateSequentialLate() // 6
        {
            if (EventFlag)
                FrameEventSequentialLate?.Invoke(this, EventArgs.Empty);
        }

        public void PostFrameTrigger() // END
        {
            if (EventFlag)
                PostFrameEvent?.Invoke(this, EventArgs.Empty);
            
            EventFlag = false;
        }

#endregion*/

    }
}
