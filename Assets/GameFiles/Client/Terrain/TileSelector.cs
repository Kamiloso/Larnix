using Larnix.Blocks;
using Larnix.Client;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using System;
using Larnix.Modules.Blocks;
using System.Linq;

namespace Larnix.Client.Terrain
{
    public class TileSelector : MonoBehaviour
    {
        [SerializeField] Camera Camera;
        [SerializeField] SpriteRenderer Selector;
        
        const float Transparency = 0.70f;
        const float BackDarking = 0.45f;

        private bool isGameFocused = true;
        private Vector2? old_cursor_pos = null;
        private bool active = false;

        private int framesAlready = 0;
        private const int MIN_FRAMES = 3;

        private const float INTERACTION_RANGE = 7f;

        private void Awake()
        {
            References.TileSelector = this;
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            isGameFocused = hasFocus;
        }

        public void FromInventoryUpdate()
        {
            if(framesAlready < MIN_FRAMES)
            {
                framesAlready++;
                return;
            }

            Vector2 mouse_pos = Input.mousePosition;
            Vector2 cursor_pos = Camera.ScreenToWorldPoint(mouse_pos);

            bool onScreen = (mouse_pos.x >= 0 && mouse_pos.x <= Screen.width && mouse_pos.y >= 0 && mouse_pos.y <= Screen.height);

            Selector.transform.localRotation = Quaternion.Euler(0, 0, 0);

            if (active && isGameFocused && onScreen) // ENABLED CURSOR
            {
                Selector.enabled = true;

                Vector2Int pointed_block = ChunkMethods.CoordsToBlock(cursor_pos);

                HashSet<Vector2Int> grids;
                if(true || References.Debug.SpectatorMode)
                {
                    grids = old_cursor_pos != null ?
                        GetCellsIntersectedByLine((Vector2)old_cursor_pos, cursor_pos) :
                        new HashSet<Vector2Int> { pointed_block };
                }
                else
                {
                    Vector2 playerpos = References.MainPlayer.GetPosition();
                    if (Common.InSquareDistance(cursor_pos, playerpos) > INTERACTION_RANGE)
                    {
                        cursor_pos = Common.ReduceIntoSquare(playerpos, cursor_pos, INTERACTION_RANGE);
                        pointed_block = ChunkMethods.CoordsToBlock(cursor_pos);
                    }

                    grids = new HashSet<Vector2Int> { pointed_block };
                }

                foreach (Vector2Int grid in grids)
                {
                    DoActionOn(grid);
                }

                old_cursor_pos = cursor_pos;
            }
            else // DISABLED CURSOR
            {
                Selector.enabled = false;
                old_cursor_pos = null;
            }
        }

        private void DoActionOn(Vector2Int pointed_block)
        {
            transform.position = (Vector2)pointed_block;

            bool hold_0 = Input.GetMouseButton(0);
            bool hold_1 = Input.GetMouseButton(1);
            bool hold_2 = Input.GetMouseButton(2);
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
                Color transpColor = new Color(1, 1, 1);
                Color darkerColor = new Color(0, 0, 0);

                bool can_be_broken = WorldAPI.CanBeBroken(pointed_block, item, !shift);

                Selector.sprite = tile.sprite;

                if(!shift) // front
                {
                    Selector.sortingLayerID = SortingLayer.NameToID("FrontBlocks");
                    Selector.color = transpColor;
                }
                else // back
                {
                    Selector.sortingLayerID = SortingLayer.NameToID("BackBlocks");
                    Selector.color = transpColor;
                    Selector.transform.localRotation = Quaternion.Euler(0, 0, -90);
                }

                if(hold_0)
                {
                    Selector.color = Color.Lerp(transpColor, darkerColor, BackDarking);
                }

                if (hold_0 && can_be_broken)
                {
                    WorldAPI.BreakBlock(pointed_block, item, !shift);
                }
            }
            else
            {
                Color transpColor = new Color(1, 1, 1, Transparency);
                Color darkerColor = new Color(0, 0, 0, Transparency);

                if (WorldAPI.CanBePlaced(pointed_block, item, !shift))
                {
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

        public static HashSet<Vector2Int> GetCellsIntersectedByLine(Vector2 start, Vector2 end)
        {
            HashSet<Vector2Int> cells = new HashSet<Vector2Int>();
            if (start == end)
            {
                cells.Add(Vector2Int.RoundToInt(start));
                return cells;
            }

            Vector2 dir = end - start;

            Vector2Int cell = Vector2Int.RoundToInt(start);
            Vector2Int cellEnd = Vector2Int.RoundToInt(end);

            int stepX = dir.x > 0 ? 1 : (dir.x < 0 ? -1 : 0);
            int stepY = dir.y > 0 ? 1 : (dir.y < 0 ? -1 : 0);

            float tDeltaX = stepX != 0 ? Mathf.Abs(1f / dir.x) : float.PositiveInfinity;
            float tDeltaY = stepY != 0 ? Mathf.Abs(1f / dir.y) : float.PositiveInfinity;

            float nextGridLineX = cell.x + (stepX > 0 ? 0.5f : -0.5f);
            float nextGridLineY = cell.y + (stepY > 0 ? 0.5f : -0.5f);

            float tMaxX = stepX != 0
                ? (nextGridLineX - start.x) / dir.x
                : float.PositiveInfinity;
            float tMaxY = stepY != 0
                ? (nextGridLineY - start.y) / dir.y
                : float.PositiveInfinity;

            cells.Add(cell);

            while (cell != cellEnd)
            {
                if (tMaxX < tMaxY)
                {
                    cell.x += stepX;
                    tMaxX += tDeltaX;
                }
                else
                {
                    cell.y += stepY;
                    tMaxY += tDeltaY;
                }
                cells.Add(cell);

                if(cells.Count > 256)
                {
                    UnityEngine.Debug.LogWarning("Trying to put over 256 cells into array!");
                    break;
                }
            }

            return cells;
        }

        public void Enable()
        {
            active = true;
            framesAlready = 0;
        }

        public void Disable()
        {
            active = false;
        }
    }
}
