using Larnix.Blocks;
using Larnix.Client;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using System;
using System.Linq;
using Larnix.Core.Vectors;
using Larnix.Core.Utils;
using Larnix.Blocks.Structs;
using Larnix.Client.Relativity;
using Larnix.Client.UI;
using Larnix.Blocks.All;
using Larnix.Core;

namespace Larnix.Client.Terrain
{
    public class TileSelector : MonoBehaviour
    {
        public const int MIN_FRAMES = 3;
        public const float INTERACTION_RANGE = 8f;
        public const float TRANSPARENCY = 0.70f;
        public const float BACK_DARKING = 0.45f;

        [SerializeField] Camera Camera;
        [SerializeField] SpriteRenderer Selector;

        private MainPlayer MainPlayer => GlobRef.Get<MainPlayer>();
        private Debugger Debugger => GlobRef.Get<Debugger>();
        private Inventory Inventory => GlobRef.Get<Inventory>();

        private bool _isGameFocused = true;
        private Vector2? _oldMousePos = null;
        private int _framesAlready = 0;
        private bool _active = false;

        private void Awake()
        {
            GlobRef.Set(this);
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            _isGameFocused = hasFocus;
        }

        public void Update2()
        {
            if(_framesAlready < MIN_FRAMES)
            {
                _framesAlready++;
                return;
            }

            Vector2 mouse_pos = Input.mousePosition;
            Vec2 cursor_pos = MainPlayer.ToLarnixPos(Camera.ScreenToWorldPoint(mouse_pos));
            Vec2? old_cursor_pos = _oldMousePos != null ? MainPlayer.ToLarnixPos(Camera.ScreenToWorldPoint((Vector2)_oldMousePos)) : null;
            Vec2 player_pos = MainPlayer.Position;

            bool pointsRight = cursor_pos.x >= player_pos.x;

            List<Vec2Int> grids = old_cursor_pos != null ?
                GetCellsIntersectedByLine((Vec2)old_cursor_pos, cursor_pos) :
                new List<Vec2Int> { BlockUtils.CoordsToBlock(cursor_pos) };

            if (!Debugger.SpectatorMode)
                grids.RemoveAll(grid => Vec2.Distance(new Vec2(grid.x, grid.y), player_pos) > INTERACTION_RANGE);

            if (_active && _isGameFocused && grids.Count > 0) // ENABLED CURSOR
            {
                Selector.transform.localRotation = Quaternion.identity; // rotation may change during DoActionOn(grid)
                Selector.transform.localScale = Vector3.one; // scale may change during DoActionOn(grid)

                foreach (Vec2Int grid in grids)
                {
                    Selector.transform.SetLarnixPos(new Vec2(grid.x, grid.y));
                    DoActionOn(grid, pointsRight);
                }

                Selector.enabled = true;
                _oldMousePos = mouse_pos;
            }
            else // DISABLED CURSOR
            {
                Selector.enabled = false;
                _oldMousePos = null;
            }
        }

        private void DoActionOn(Vec2Int pointed_block, bool pointsRight)
        {
            bool hold_0 = Input.GetMouseButton(0);
            bool hold_1 = Input.GetMouseButton(1);
            bool hold_2 = Input.GetMouseButton(2);
            bool shift = Input.GetKey(KeyCode.LeftShift);

            BlockData1 item = Inventory.GetHoldingItem();
            bool is_tool = BlockFactory.HasInterface<ITool>(item.ID);

            Action HideSelector = () =>
            {
                Tile tile = Tiles.GetTile(new BlockData1(), true);
                Selector.sprite = tile.sprite;
            };

            Tile tile = Tiles.GetTile(item, !shift);

            if (is_tool)
            {
                bool can_be_broken = TerrainAPI.CanBeBroken(pointed_block, item, !shift);

                Selector.sprite = tile.sprite;
                Selector.color = new Color(1, 1, 1);
                Selector.transform.localScale = new Vector3(pointsRight ? 1f : -1f, 1f, 1f) * 0.9f;
                Selector.transform.localRotation = Quaternion.Euler(0f, 0f, (shift ? 1f : 0f) * (pointsRight ? -1f : 1f) * 90f);
                Selector.sortingLayerID = !shift ? SortingLayer.NameToID("FrontBlocks") : SortingLayer.NameToID("BackBlocks");

                if (hold_0 && can_be_broken)
                {
                    TerrainAPI.BreakBlock(pointed_block, item, !shift);
                }
            }
            else
            {
                Color transpColor = new Color(1, 1, 1, TRANSPARENCY);
                Color darkerColor = new Color(0, 0, 0, TRANSPARENCY);

                if (TerrainAPI.CanBePlaced(pointed_block, item, !shift))
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
                        Selector.color = Color.Lerp(transpColor, darkerColor, BACK_DARKING);
                    }

                    if (hold_0)
                    {
                        TerrainAPI.PlaceBlock(pointed_block, item, !shift);
                    }
                }
                else HideSelector();
            }
        }

        public static List<Vec2Int> GetCellsIntersectedByLine(Vec2 start, Vec2 end)
        {
            double magnitude = Vec2.Distance(start, end);
            if (magnitude > 256.0) return new();
            if (magnitude == 0.0) return new() { BlockUtils.CoordsToBlock(start) };

            const int ACCURACY = 40;
            int segments = (int)Math.Ceiling(ACCURACY * magnitude);
            Vec2 difference = end - start;
            Vec2 roadpart = difference / segments;

            HashSet<Vec2Int> tiles = new();
            for (int i = 0; i <= segments; i++)
            {
                Vec2Int block = BlockUtils.CoordsToBlock(start + i * roadpart);
                tiles.Add(block);
            }

            return tiles.ToList();
        }

        public void Enable()
        {
            _active = true;
            _framesAlready = 0;
        }

        public void Disable()
        {
            _active = false;
        }
    }
}
