#nullable enable
using System.Collections.Generic;
using System.Linq;
using Larnix.Core.Vectors;
using Larnix.Model.Utils;

namespace Larnix.Model.Physics.Collections;

internal class SpatialDictionary<T> where T : class
{
    private readonly Dictionary<Vec2Int, List<(Vec2, T)>> _dict = new();
    private readonly double _sectorSize;

    public SpatialDictionary(double sectorSize)
    {
        _sectorSize = sectorSize;
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
            _dict.Remove(sector);
    }

    public void Move(Vec2 oldPos, Vec2 newPos, T item)
    {
        RemoveByReference(oldPos, item);
        Add(newPos, item);
    }

    public List<T> Get3x3SectorList(Vec2 pos)
    {
        List<T> returns = new();

        var center = BlockUtils.CoordsToBlock(pos, _sectorSize);
        for (int dx = -1; dx <= 1; dx++)
            for (int dy = -1; dy <= 1; dy++)
            {
                var sector = center + new Vec2Int(dx, dy);
                if (_dict.TryGetValue(sector, out var list))
                {
                    returns.AddRange(list.Select(p => p.Item2));
                }
            }

        return returns;
    }

    private List<(Vec2, T)> GetSectorList(Vec2 pos, out Vec2Int sector)
    {
        var _sector = BlockUtils.CoordsToBlock(pos, _sectorSize);
        if (!_dict.TryGetValue(_sector, out var list))
        {
            list = new();
            _dict[_sector] = list;
        }

        sector = _sector;
        return list;
    }
}
