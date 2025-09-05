using Larnix.Blocks;
using Larnix.Client;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using System;
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
        private Vector2? old_mouse_pos = null;
        private bool active = false;

        private int framesAlready = 0;
        private const int MIN_FRAMES = 3;

        private const float INTERACTION_RANGE = 8f;

        private void Awake()
        {
            Ref.TileSelector = this;
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
            Vector2? old_cursor_pos = old_mouse_pos != null ? Camera.ScreenToWorldPoint((Vector2)old_mouse_pos) : null;
            Vector2 player_pos = Ref.MainPlayer.GetPosition();

            bool pointsRight = cursor_pos.x >= player_pos.x;

            List<Vector2Int> grids = old_cursor_pos != null ?
                GetCellsIntersectedByLine((Vector2)old_cursor_pos, cursor_pos) :
                new List<Vector2Int> { ChunkMethods.CoordsToBlock(cursor_pos) };

            if (!Ref.Debug.SpectatorMode)
                grids.RemoveAll(grid => Vector2.Distance(grid, player_pos) > INTERACTION_RANGE);

            if (active && isGameFocused && grids.Count > 0) // ENABLED CURSOR
            {
                Selector.transform.localRotation = Quaternion.identity; // rotation may change during DoActionOn(grid)
                Selector.transform.localScale = Vector3.one; // scale may change during DoActionOn(grid)

                foreach (Vector2Int grid in grids)
                {
                    Selector.transform.position = (Vector2)grid;
                    DoActionOn(grid, pointsRight);
                }

                Selector.enabled = true;
                old_mouse_pos = mouse_pos;
            }
            else // DISABLED CURSOR
            {
                Selector.enabled = false;
                old_mouse_pos = null;
            }
        }

        private void DoActionOn(Vector2Int pointed_block, bool pointsRight)
        {
            bool hold_0 = Input.GetMouseButton(0);
            bool hold_1 = Input.GetMouseButton(1);
            bool hold_2 = Input.GetMouseButton(2);
            bool shift = Input.GetKey(KeyCode.LeftShift);

            BlockData1 item = Ref.Inventory.GetHoldingItem();
            bool is_tool = BlockFactory.HasInterface<ITool>(item.ID);

            Action HideSelector = () =>
            {
                Tile tile = Tiles.GetTile(new BlockData1 { }, true);
                Selector.sprite = tile.sprite;
            };

            Tile tile = Tiles.GetTile(item, !shift);

            if (is_tool)
            {
                bool can_be_broken = WorldAPI.CanBeBroken(pointed_block, item, !shift);

                Selector.sprite = tile.sprite;
                Selector.color = new Color(1, 1, 1);
                Selector.transform.localScale = new Vector3(pointsRight ? 1f : -1f, 1f, 1f) * 0.9f;
                Selector.transform.localRotation = Quaternion.Euler(0f, 0f, (shift ? 1f : 0f) * (pointsRight ? -1f : 1f) * 90f);
                Selector.sortingLayerID = !shift ? SortingLayer.NameToID("FrontBlocks") : SortingLayer.NameToID("BackBlocks");

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

        public static List<Vector2Int> GetCellsIntersectedByLine(Vector2 start, Vector2 end)
        {
            List<Vector2Int> cells = new List<Vector2Int>();
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
