using Larnix.Blocks;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

        public static readonly SingleBlockData StaticAirBlock = new SingleBlockData { };

        private void Awake()
        {
            References.Inventory = this;
        }

        private void Update()
        {
            if(!Input.GetKey(KeyCode.LeftControl))
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

            References.TileSelector.FromInventoryUpdate();
        }

        public SingleBlockData GetHoldingItem()
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
                Block = new SingleBlockData { ID = blockID, Variant = variant },
                Count = blockID == BlockID.Air ? 0 : 1
            };
        }
    }
}
