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

        public event EventHandler PreFrameEvent;
        public event EventHandler FrameEventRandom;
        public event EventHandler FrameEventSequential;

        public BlockServer(Vector2Int position, SingleBlockData blockData, bool isFront)
        {
            Position = position;
            BlockData = blockData;
            IsFront = isFront;
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
