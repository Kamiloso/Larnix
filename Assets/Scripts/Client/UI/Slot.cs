using Larnix.Blocks;
using Larnix.Client.Terrain;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Larnix.Blocks.Structs;
using Larnix.Blocks.All;

namespace Larnix.Client.UI
{
    public class Slot : MonoBehaviour
    {
        private const float DISPLAY_TIME = 1.2f;
        private const float FADE_TIME = 0.6f;

        [SerializeField] Image Image;
        [SerializeField] TextMeshProUGUI Title;
        [SerializeField] GameObject Selected;
        [SerializeField] Color TitleColor;

        private Inventory Inventory => Ref.Inventory;

        private float _textDisplayTime = 0f;
        private int _slotID = -1;

        public void Init(int slotID)
        {
            _slotID = slotID;
        }

        private void LateUpdate()
        {
            bool slotActive = _slotID == Inventory.SelectedSlot;
            if (Selected.activeSelf)
            {
                if(!slotActive)
                {
                    _textDisplayTime = 0f;
                    Selected.SetActive(false);
                }
            }
            else
            {
                if(slotActive)
                {
                    _textDisplayTime = DISPLAY_TIME;
                    Selected.SetActive(true);
                }
            }

            Item item = Inventory.GetItemInSlot(_slotID);
            if(item.Count != 0)
            {
                Image.sprite = Tiles.GetSprite(item.Block, true);
                Title.text = _textDisplayTime > 0f ?
                    BlockFactory.GetSlaveInstance<IBlockInterface>(item.Block.ID)?.STATIC_GetBlockName(item.Block.Variant) ?? string.Empty :
                    string.Empty;
            }
            else
            {
                Image.sprite = Tiles.GetSprite(new BlockData1(), true);
                Title.text = string.Empty;
            }

            if (_textDisplayTime > 0f)
            {
                Title.gameObject.SetActive(true);
                Title.color = _textDisplayTime > FADE_TIME ? TitleColor : new Color(TitleColor.r, TitleColor.g, TitleColor.b, _textDisplayTime / FADE_TIME);
                _textDisplayTime -= Time.deltaTime;
            }
            else
            {
                Title.gameObject.SetActive(false);
            }
        }
    }
}
