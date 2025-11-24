using Larnix.Blocks;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Larnix.Blocks.Structs;

namespace Larnix.Client.UI
{
    public class Inventory : MonoBehaviour
    {
        [SerializeField] BlockID[] BlocksInSlots;
        [SerializeField] byte[] VariantsInSlots;

        public int SelectedSlot { get; private set; } = 0;
        private const int MIN_SLOT = 0;
        private const int MAX_SLOT = 9;
        private const int MIN_SELECTABLE = 0;
        private const int MAX_SELECTABLE = 9;

        public static readonly BlockData1 StaticAirBlock = new BlockData1 { };

        private void Awake()
        {
            Ref.Inventory = this;
        }

        public void Update1()
        {
            if (!Input.GetKey(KeyCode.LeftControl))
            {
                float scroll = Input.GetAxis("Mouse ScrollWheel");
                SelectedSlot -= System.Math.Sign(scroll);
            }

            for (int slot = MIN_SELECTABLE; slot <= MAX_SELECTABLE; slot++)
            {
                if (slot < 0 || slot > 9) continue;

                KeyCode slotKey = slot != 9 ? (KeyCode)((int)KeyCode.Alpha1 + slot) : KeyCode.Alpha0;
                if(Input.GetKeyDown(slotKey))
                    SelectedSlot = slot;
            }

            if (SelectedSlot < MIN_SELECTABLE)
                SelectedSlot = MAX_SELECTABLE;

            if (SelectedSlot > MAX_SELECTABLE)
                SelectedSlot = MIN_SELECTABLE;

            if (Ref.Debug.ClientBlockSwap)
            {
                int deltaBlock = (Input.GetKeyDown(KeyCode.P) ? 1 : 0) - (Input.GetKeyDown(KeyCode.O) ? 1 : 0);
                int deltaVariant = (Input.GetKeyDown(KeyCode.L) ? 1 : 0) - (Input.GetKeyDown(KeyCode.K) ? 1 : 0);

                BlocksInSlots[SelectedSlot] = (BlockID)((int)BlocksInSlots[SelectedSlot] + deltaBlock);
                VariantsInSlots[SelectedSlot] += (byte)deltaVariant;
                VariantsInSlots[SelectedSlot] %= 16;
            }
        }

        public BlockData1 GetHoldingItem()
        {
            Item item = GetItemInSlot(SelectedSlot);
            return item.Count != 0 ? item.Block : StaticAirBlock;
        }

        public Item GetItemInSlot(int slotID)
        {
            BlockID blockID;
            byte variant;

            try
            {
                blockID = BlocksInSlots[slotID];
                variant = VariantsInSlots[slotID];
            }
            catch(IndexOutOfRangeException)
            {
                blockID = BlockID.Air;
                variant = 0;
            }

            return new Item
            {
                Block = new BlockData1 { ID = blockID, Variant = variant },
                Count = blockID == BlockID.Air ? 0 : 1
            };
        }
    }
}
