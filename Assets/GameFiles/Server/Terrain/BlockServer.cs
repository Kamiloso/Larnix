using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Larnix.Blocks;
using System;

namespace Larnix.Server.Terrain
{
    public class BlockServer
    {
        public readonly Vector2Int Position;
        public readonly bool IsFront;
        public SingleBlockData BlockData;

        private bool MarkedToUpdate = false;
        private bool StateChanged = false;

        public event EventHandler FrameEvent;
        public event EventHandler BlockUpdateEvent;

        public BlockServer(Vector2Int position, SingleBlockData blockData, bool isFront)
        {
            Position = position;
            BlockData = blockData;
            IsFront = isFront;
        }

        public void PreFrameConfigure()
        {
            MarkedToUpdate = true;
        }

        public void BlockUpdate()
        {
            StateChanged = true;
        }

        public void FrameUpdate()
        {
            if (MarkedToUpdate)
            {
                if(StateChanged)
                    BlockUpdateEvent?.Invoke(this, EventArgs.Empty);
                StateChanged = false;

                FrameEvent?.Invoke(this, EventArgs.Empty);
            }
            MarkedToUpdate = false;
        }
    }
}
