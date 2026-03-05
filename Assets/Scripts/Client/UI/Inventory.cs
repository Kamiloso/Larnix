using Larnix.Blocks;
using System;
using Larnix.Scoping;
using UnityEngine;
using Larnix.Blocks.Structs;
using Larnix.Core;

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

        private Debugger Debugger => GlobRef.Get<Debugger>();

        private int SlotCount => MaxSlot - MinSlot + 1;
        public int SelectedSlot { get; private set; } = 0;

        private void Awake()
        {
            GlobRef.Set(this);
        }

        public void Update1()
        {
            float scroll = MyInput.GetScrollNormal();
            SelectedSlot -= Math.Sign(scroll);

            for (int slot = MinSelectable; slot <= MaxSelectable; slot++)
            {
                if (slot < MinSlot || slot > MaxSlot)
                    continue;

                KeyCode slotKey = slot != 9 ?
                    (KeyCode)((int)KeyCode.Alpha1 + slot) : KeyCode.Alpha0;
                
                if (MyInput.GetKeyDown(slotKey))
                    SelectedSlot = slot;
            }

            while (SelectedSlot < MinSelectable) SelectedSlot += SlotCount;
            while (SelectedSlot > MaxSelectable) SelectedSlot -= SlotCount;

            if (Debugger.ClientBlockSwap)
            {
                int deltaBlock = (MyInput.GetKeyDown(KeyCode.P) ? 1 : 0) - (MyInput.GetKeyDown(KeyCode.O) ? 1 : 0);
                int deltaVariant = (MyInput.GetKeyDown(KeyCode.L) ? 1 : 0) - (MyInput.GetKeyDown(KeyCode.K) ? 1 : 0);

                BlocksInSlots[SelectedSlot] = (BlockID)((int)BlocksInSlots[SelectedSlot] + deltaBlock);
                VariantsInSlots[SelectedSlot] += (byte)deltaVariant;
                VariantsInSlots[SelectedSlot] %= 16;
            }
        }

        public BlockData1 GetHoldingBlock()
        {
            Item item = GetItemInSlot(SelectedSlot);
            return item.Count != 0 ? item.Block : BlockData1.Air;
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
            catch (IndexOutOfRangeException)
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
