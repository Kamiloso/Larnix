using Larnix.Model.Blocks;
using Larnix.Scoping;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using System;
using System.Linq;
using Larnix.Core.Vectors;
using Larnix.Model.Utils;
using Larnix.Client.UI;
using Larnix.Model.Blocks.All;
using Larnix.Core;
using Larnix.Client.Graphics;
using Larnix.Model.Blocks.Structs;

namespace Larnix.Client.Terrain.Selector
{
    public class TileSelector : MonoBehaviour
    {
        private const float INTERACTION_RANGE = 8f;

        [SerializeField] Camera Camera;
        [SerializeField] SelectorDisplay Selector;

        private Client Client => GlobRef.Get<Client>();
        private MainPlayer MainPlayer => GlobRef.Get<MainPlayer>();
        private Inventory Inventory => GlobRef.Get<Inventory>();
        private Debugger Debugger => GlobRef.Get<Debugger>();

        private bool _active = false; // start inactive
        private Vec2? _oldCursorPos = null;

        private void Awake()
        {
            GlobRef.Set(this);
        }

        private void Start()
        {
            MainPlayer.OnRespawn += () =>
            {
                _active = true;
                _oldCursorPos = null;
            };

            MainPlayer.OnDeath += () =>
            {
                _active = false;
            };
        }

        public void Update2()
        {
            Selector.Prepare();

            if (Client.IsGameFocused && _active)
            {
                Vec2? cursorPosNullable = MyInput.MouseTargetPos();
                if (cursorPosNullable != null)
                {
                    Vec2 cursorPos = cursorPosNullable.Value;
                    bool pointerActive = PointerAction(_oldCursorPos ?? cursorPos, cursorPos);

                    if (pointerActive)
                    {
                        Vec2Int POS = BlockUtils.CoordsToBlock(cursorPos);
                        Selector.ShowAt(POS);
                        _oldCursorPos = cursorPos;
                        return;
                    }
                }
            }

            Selector.Hide();
            _oldCursorPos = null;
        }

        /// <summary>
        /// Returns whether the pointer should be visible.
        /// </summary>
        private bool PointerAction(Vec2 p1, Vec2 p2)
        {
            Vec2 playerPos = MainPlayer.Position;

            List<Vec2Int> allGrids = GetCellsIntersectedByLine(p1, p2);
            List<Vec2Int> nearbyGrids = allGrids
                .Where(grid => Vec2.Distance(grid, playerPos) <= INTERACTION_RANGE || Debugger.SpectatorMode)
                .ToList();

            // Hide when not all grids are nearby
            if (allGrids.Count != nearbyGrids.Count)
            {
                return false;
            }

            bool pointsRight = p2.x >= playerPos.x;

            foreach (Vec2Int grid in allGrids)
            {
                DoActionOn(grid, pointsRight);
            }

            return true;
        }

        private void DoActionOn(Vec2Int pointedBlock, bool pointsRight)
        {
            bool click = MyInput.PressClickLeft;
            bool shift = MyInput.PressCrouch;

            BlockHeader1 holdBlock = Inventory.GetHoldingBlock().Header;
            Tile tile = Tiles.GetTile(holdBlock, !shift);

            bool isTool = BlockFactory.HasInterface<ITool>(holdBlock.ID);

            if (isTool)
            {
                Selector.DisplayTool(tile, pointsRight, !shift);

                if (TerrainAPI.CanBeBroken(pointedBlock, holdBlock, !shift))
                {
                    if (click)
                    {
                        TerrainAPI.BreakBlock(pointedBlock, holdBlock, !shift);
                    }
                }
            }
            else
            {
                if (TerrainAPI.CanBePlaced(pointedBlock, holdBlock, !shift))
                {
                    Selector.DisplayBlockPreview(tile, !shift);

                    if (click)
                    {
                        TerrainAPI.PlaceBlock(pointedBlock, holdBlock, !shift);
                    }
                }
                else
                {
                    Selector.DisplayEmpty();
                }
            }
        }

        public static List<Vec2Int> GetCellsIntersectedByLine(Vec2 start, Vec2 end)
        {
            double magnitude = Vec2.Distance(start, end);
            if (magnitude > 250.0) return new();
            if (magnitude == 0.0) return new() { BlockUtils.CoordsToBlock(start) };

            const int ACCURACY = 50;
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
    }
}
