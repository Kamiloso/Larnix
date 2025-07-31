using Larnix.Blocks;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Larnix.Client
{
    public class Inventory : MonoBehaviour
    {
        private BlockID TempHolding;

        private void Awake()
        {
            References.Inventory = this;
        }

        private void Update()
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");

            if(scroll < 0f)
            {
                TempHolding++;
            }

            if(scroll > 0f)
            {
                TempHolding--;
            }

            if (TempHolding == (BlockID)ushort.MaxValue)
                TempHolding++;

            if (!Enum.IsDefined(typeof(BlockID), TempHolding))
                TempHolding--;

            References.TileSelector.FromInventoryUpdate();
        }

        public SingleBlockData GetHoldingItem()
        {
            return new SingleBlockData { ID = TempHolding };
        }
    }
}
