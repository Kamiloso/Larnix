using Larnix.Blocks;
using Larnix.Client.Terrain;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Larnix.Client.UI
{
    public class Slot : MonoBehaviour
    {
        [SerializeField] Image Image;
        [SerializeField] GameObject Selected;

        private int SlotID = -1;

        public void Init(int slotID)
        {
            SlotID = slotID;
        }

        private void LateUpdate()
        {
            Item item = References.Inventory.GetItemInSlot(SlotID);
            Image.sprite = item.Count != 0 ? Tiles.GetSprite(item.Block, true) : Tiles.GetSprite(Inventory.StaticAirBlock, true);
            Selected.SetActive(SlotID == References.Inventory.SelectedSlot);
        }
    }
}
