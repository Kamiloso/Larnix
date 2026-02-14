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
        public Vec2Int Position { get; init; }
        public bool IsFront { get; init; }
        public BlockData1 BlockData { get; init; } // connected to block-saving system

        internal BlockServer(Vec2Int position, BlockData1 blockData, bool isFront)
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
    }
}
