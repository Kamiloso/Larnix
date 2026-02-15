using System.Collections;
using System.Collections.Generic;
using Larnix.Blocks;
using System;
using Larnix.Blocks.Structs;
using Larnix.Core.Vectors;
using Larnix.Core.Utils;

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

        private BlockEvents _eventSystem = null;

        /// <summary>
        /// Determines whether block is active for event in the current frame.
        /// Resets to false when ID or Variant is changed. Can be prevented
        /// by replacing block with BreakMode = Weak.
        /// </summary>
        public bool EventFlag { get; set; }
        private readonly Action[] _actions = new Action[Enum.GetValues(typeof(BlockEvent)).Length];

        internal void Subscribe(BlockEvent type, Action action)
        {
            if (_eventSystem != null)
                throw new InvalidOperationException("Cannot subscribe to events after attaching to event system.");

            Action prev = _actions[(int)type] ?? (() => { });
            _actions[(int)type] = prev + (() => InvokeEvent(type, action));
        }

        private void InvokeEvent(BlockEvent type, Action action)
        {
            if (_eventSystem == null)
                return; // not attached, ignore

            action();
        }

        public void AttachTo(BlockEvents eventSystem)
        {
            _eventSystem = eventSystem;
            
            for (int i = 0; i < _actions.Length; i++)
            {
                var action = _actions[i];
                if (action != null)
                {
                    Vec2Int pos = BlockUtils.LocalBlockCoords(Position);
                    bool front = IsFront;
                    
                    _eventSystem.Subscribe(pos, front, (BlockEvent)i, action);
                }
            }
        }

        public void Detach()
        {
            for (int i = 0; i < _actions.Length; i++)
            {
                var action = _actions[i];
                if (action != null)
                {
                    _eventSystem.Unsubscribe((BlockEvent)i, action);
                }
            }

            _eventSystem = null;
        }
    }
}
