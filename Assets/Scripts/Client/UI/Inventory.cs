using Larnix.Blocks;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Larnix.Blocks.Structs;
using Larnix.Client.Terrain;

namespace Larnix.Client.UI
{
    public class Inventory : MonoBehaviour
    {
        [SerializeField] int MinSlot = 0;
        [SerializeField] int MaxSlot = 9;
        [SerializeField] int MinSelectable = 0;
        [SerializeField] int MaxSelectable = 9;

        [SerializeField] BlockID[] BlocksInSlots;
        [SerializeField] byte[] VariantsInSlots;

        private Debugger Debugger => Ref.Debugger;

        public int SelectedSlot { get; private set; } = 0;

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

            for (int slot = MinSelectable; slot <= MaxSelectable; slot++)
            {
                if (slot < MinSlot || slot > MaxSlot) continue;

                KeyCode slotKey = slot != 9 ? (KeyCode)((int)KeyCode.Alpha1 + slot) : KeyCode.Alpha0;
                if(Input.GetKeyDown(slotKey))
                    SelectedSlot = slot;
            }

            if (SelectedSlot < MinSelectable)
                SelectedSlot = MaxSelectable;

            if (SelectedSlot > MaxSelectable)
                SelectedSlot = MinSelectable;

            if (Debugger.ClientBlockSwap)
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
            return item.Count != 0 ? item.Block : new BlockData1();
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

            return new Item(
                block: new(blockID, variant),
                count: blockID == BlockID.Air ? 0 : 1
                );
        }
    }
}
