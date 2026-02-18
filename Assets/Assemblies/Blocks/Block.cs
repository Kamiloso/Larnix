using System.Collections;
using System.Collections.Generic;
using Larnix.Blocks;
using System;
using Larnix.Blocks.Structs;
using Larnix.Core.Vectors;
using Larnix.Core.Utils;

namespace Larnix.Blocks
{
    public class Block
    {
        public Vec2Int Position { get; private set; }
        public bool IsFront { get; private set; }
        public BlockData1 BlockData { get; private set; } // connected to block-saving system
        public IWorldAPI WorldAPI { get; private set; }

        private bool _constructed = false;

        internal Block() {}
        public record BlockInits(Vec2Int Position, bool IsFront, BlockData1 BlockData, IWorldAPI WorldAPI);
        internal void Construct(BlockInits blockInits)
        {
            if (!_constructed)
            {
                Position = blockInits.Position;
                IsFront = blockInits.IsFront;
                BlockData = blockInits.BlockData;
                WorldAPI = blockInits.WorldAPI;

                _constructed = true;
            }
            else throw new InvalidOperationException("Block already constructed.");
        }

        private BlockEvents _eventSystem = null;

        /// <summary>
        /// Determines whether block is active for event in the current frame.
        /// Resets to false when ID or Variant is changed. Can be prevented
        /// by replacing block with BreakMode = Weak.
        /// </summary>
        public bool EventFlag { get; set; } = false;
        private readonly Action[] _actions = new Action[Enum.GetValues(typeof(BlockOrder)).Length];

        internal void Subscribe(BlockOrder type, Action action)
        {
            if (_eventSystem != null)
                throw new InvalidOperationException("Cannot subscribe to events after attaching to event system.");

            Action prev = _actions[(int)type] ?? (() => { });
            _actions[(int)type] = prev + (() => {
                if (EventFlag)
                {
                    InvokeEvent(type, action);
                }
                });
        }

        private void InvokeEvent(BlockOrder type, Action action)
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
                    
                    _eventSystem.Subscribe(pos, front, (BlockOrder)i, action);
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
                    _eventSystem.Unsubscribe((BlockOrder)i, action);
                }
            }

            _eventSystem = null;
        }
    }
}
