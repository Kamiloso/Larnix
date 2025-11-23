using Larnix.Core;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Larnix.Entities
{
    public class EntityNBT : NBT
    {
        public enum ActionType : ushort
        {
            None = 0,
            WalkingRight = 1,
            WalkingLeft = 2,
        }

        public EntityNBT(int capacity = 0) : base(capacity) { Initialize(); }
        public EntityNBT(byte[] data) : base(data) { Initialize(); }

        private void Initialize()
        {
            Define<byte>("marked_to_kill", 0);
            Define<int>("next_action_ticks", 1);
            Define<ActionType>("action", 5);
        }

        public bool MarkedToKill
        {
            set => Set<byte>("marked_to_kill", (byte)(value ? 1 : 0));
            get => Get<byte>("marked_to_kill") == 1;
        }

        public int NextActionTicks
        {
            set => Set<int>("next_action_ticks", value);
            get => Get<int>("next_action_ticks");
        }

        public ActionType Action
        {
            set => Set<ActionType>("action", value);
            get => Get<ActionType>("action");
        }
    }
}
