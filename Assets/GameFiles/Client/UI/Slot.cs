using Larnix.Blocks;
using Larnix.Client.Terrain;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Larnix.Client.UI
{
    public class Slot : MonoBehaviour
    {
        [SerializeField] Image Image;
        [SerializeField] TextMeshProUGUI Title;
        [SerializeField] GameObject Selected;
        [SerializeField] Color TitleColor;

        private int SlotID = -1;

        private float TextDisplayTime = 0f;
        private const float DISPLAY_TIME = 1.2f;
        private const float FADE_TIME = 0.6f;

        public void Init(int slotID)
        {
            SlotID = slotID;
        }

        private void LateUpdate()
        {
            bool slotActive = SlotID == Ref.Inventory.SelectedSlot;
            if (Selected.activeSelf)
            {
                if(!slotActive)
                {
                    TextDisplayTime = 0f;
                    Selected.SetActive(false);
                }
            }
            else
            {
                if(slotActive)
                {
                    TextDisplayTime = DISPLAY_TIME;
                    Selected.SetActive(true);
                }
            }

            Item item = Ref.Inventory.GetItemInSlot(SlotID);
            if(item.Count != 0)
            {
                Image.sprite = Tiles.GetSprite(item.Block, true);
                Title.text = TextDisplayTime > 0f ? Translations.GetBlockName(item.Block) : string.Empty;
            }
            else
            {
                Image.sprite = Tiles.GetSprite(Inventory.StaticAirBlock, true);
                Title.text = string.Empty;
            }

            if (TextDisplayTime > 0f)
            {
                Title.gameObject.SetActive(true);
                Title.color = TextDisplayTime > FADE_TIME ? TitleColor : new Color(TitleColor.r, TitleColor.g, TitleColor.b, TextDisplayTime / FADE_TIME);
                TextDisplayTime -= Time.deltaTime;
            }
            else
            {
                Title.gameObject.SetActive(false);
            }
        }
    }
}
