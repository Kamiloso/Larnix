using Larnix.Blocks;
using Larnix.Client;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using System;
using UnityEngine.Playables;
using Larnix.Server.Terrain;
using Larnix.Modules.Blocks;

namespace Larnix.Client.Terrain
{
    public class TileSelector : MonoBehaviour
    {
        [SerializeField] Camera Camera;
        [SerializeField] SpriteRenderer Selector;
        
        const float Transparency = 0.70f;
        const float BackDarking = 0.45f;

        private bool active = false;

        private void Awake()
        {
            References.TileSelector = this;
        }

        public void FromInventoryUpdate()
        {
            Selector.enabled = active;

            if (active)
            {
                Vector2 mouse_pos = Input.mousePosition;
                Vector2 cursor_pos = Camera.ScreenToWorldPoint(mouse_pos);

                Vector2Int pointed_block = ChunkMethods.CoordsToBlock(cursor_pos);
                transform.position = (Vector2)pointed_block;

                bool hold_0 = Input.GetMouseButton(0);
                bool hold_1 = Input.GetMouseButton(1);
                bool shift = Input.GetKey(KeyCode.LeftShift);

                SingleBlockData item = References.Inventory.GetHoldingItem();
                bool is_tool = BlockFactory.HasInterface<ITool>(item.ID);

                Action HideSelector = () =>
                {
                    Tile tile = Tiles.GetTile(new SingleBlockData { }, true);
                    Selector.sprite = tile.sprite;
                };

                Tile tile = Tiles.GetTile(item, !shift);

                if (is_tool)
                {
                    if (WorldAPI.CanBeBroken(pointed_block, item, !shift))
                    {
                        Color toolColor = new Color(1, 1, 1, 1);

                        Selector.sprite = tile.sprite;
                        Selector.sortingLayerID = SortingLayer.NameToID("OnTop");
                        Selector.color = toolColor;

                        if(hold_0)
                        {
                            WorldAPI.BreakBlock(pointed_block, item, !shift);
                        }
                    }
                    else HideSelector();
                }
                else
                {
                    if (WorldAPI.CanBePlaced(pointed_block, item, !shift))
                    {
                        Color transpColor = new Color(1, 1, 1, Transparency);
                        Color darkerColor = new Color(0, 0, 0, Transparency);

                        Selector.sprite = tile.sprite;

                        if (!shift) // front
                        {
                            Selector.sortingLayerID = SortingLayer.NameToID("FrontBlocks");
                            Selector.color = transpColor;
                        }
                        else // back
                        {
                            Selector.sortingLayerID = SortingLayer.NameToID("BackBlocks");
                            Selector.color = Color.Lerp(transpColor, darkerColor, BackDarking);
                        }

                        if (hold_0)
                        {
                            WorldAPI.PlaceBlock(pointed_block, item, !shift);
                        }
                    }
                    else HideSelector();
                }
            }
        }

        public void Enable()
        {
            active = true;
        }

        public void Disable()
        {
            active = false;
        }
    }
}
