using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine;
using Larnix.Blocks;
using UnityEngine.UIElements;

namespace Larnix.Physics
{
    public class SpatialDictionary<T> where T : class
    {
        public readonly double SectorSize;
        private readonly Dictionary<Vector2Int, List<(Vec2, T)>> InternalDictionary = new();

        public SpatialDictionary(double sectorSize)
        {
            SectorSize = sectorSize;
        }

        public void Add(Vec2 pos, T item)
        {
            var list = GetSectorList(pos, out var sector);
            list.Add((pos, item));
        }

        public void RemoveByReference(Vec2 pos, T item)
        {
            var list = GetSectorList(pos, out var sector);
            list.RemoveAll(pair => ReferenceEquals(pair.Item2, item));

            if (list.Count == 0)
                InternalDictionary.Remove(sector);
        }

        public void Move(Vec2 oldPos, Vec2 newPos, T item)
        {
            RemoveByReference(oldPos, item);
            Add(newPos, item);
        }

        public List<T> Get3x3SectorList(Vec2 pos)
        {
            List<T> returns = new();

            var center = ConvertToSector(pos);
            for (int dx = -1; dx <= 1; dx++)
                for (int dy = -1; dy <= 1; dy++)
                {
                    var sector = center + new Vector2Int(dx, dy);
                    if (InternalDictionary.TryGetValue(sector, out var list))
                    {
                        returns.AddRange(list.Select(p => p.Item2));
                    }
                }

            return returns;
        }

        private List<(Vec2, T)> GetSectorList(Vec2 pos, out Vector2Int sector)
        {
            var _sector = ConvertToSector(pos);
            if (!InternalDictionary.TryGetValue(_sector, out var list))
            {
                list = new();
                InternalDictionary[_sector] = list;
            }

            sector = _sector;
            return list;
        }

        private Vector2Int ConvertToSector(Vec2 pos)
        {
            return ChunkMethods.CoordsToBlock(pos, SectorSize);
        }
    }
}
