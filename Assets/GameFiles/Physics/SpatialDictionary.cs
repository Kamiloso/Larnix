using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Larnix.Physics
{
    public class SpatialDictionary<T>
    {
        public readonly float SectorSize;
        private readonly Dictionary<Vector2Int, List<(Vector2, T)>> InternalDictionary = new();

        public SpatialDictionary(float sectorSize)
        {
            SectorSize = sectorSize;
        }

        public void Add(Vector2 pos, T item)
        {
            var list = GetSectorListPrivate(pos, out var sector);
            list.Add((pos, item));
        }

        public void RemoveByReference(Vector2 pos, T item)
        {
            var list = GetSectorListPrivate(pos, out var sector);
            list.RemoveAll(pair => ReferenceEquals(pair.Item2, item));

            if (list.Count == 0)
                InternalDictionary.Remove(sector);
        }

        public void Move(Vector2 oldPos, Vector2 newPos, T item)
        {
            RemoveByReference(oldPos, item);
            Add(newPos, item);
        }

        public List<T> Get3x3SectorList(Vector2 pos)
        {
            List<T> returns = new();

            Vector2Int center = ConvertToSector(pos);
            for (int dx = -1; dx <= 1; dx++)
                for (int dy = -1; dy <= 1; dy++)
                {
                    Vector2Int sector = center + new Vector2Int(dx, dy);
                    if (InternalDictionary.TryGetValue(sector, out var list))
                    {
                        returns.AddRange(list.Select(p => p.Item2));
                    }
                }

            return returns;
        }

        private List<(Vector2, T)> GetSectorListPrivate(Vector2 pos, out Vector2Int sector)
        {
            Vector2Int _sector = ConvertToSector(pos);
            if (!InternalDictionary.TryGetValue(_sector, out var list))
            {
                list = new();
                InternalDictionary[_sector] = list;
            }

            sector = _sector;
            return list;
        }

        private Vector2Int ConvertToSector(Vector2 pos)
        {
            return new Vector2Int(
                Mathf.RoundToInt(pos.x / SectorSize),
                Mathf.RoundToInt(pos.y / SectorSize)
                );
        }
    }
}
