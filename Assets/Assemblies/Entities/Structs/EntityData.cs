using Larnix.Core.Vectors;
using Larnix.GameCore.Json;
using Larnix.GameCore.Enums;
using Larnix.GameCore.Structs;

namespace Larnix.Entities.Structs;

public class EntityData
{
    public EntityHeader Header { get; private set; }
    public Storage NBT { get; }

    public EntityID ID => Header.ID;
    public Vec2 Position
    {
        get => Header.Position;
        set => Header = new EntityHeader(ID, value, Header.Rotation);
    }
    public float Rotation
    {
        get => Header.Rotation;
        set => Header = new EntityHeader(ID, Header.Position, value);
    }

    public EntityData(in EntityHeader header, Storage nbt = null)
    {
        Header = header;
        NBT = nbt ?? new();
    }

    public EntityData(EntityID id, Vec2 position, float rotation, Storage nbt = null)
    {
        Header = new EntityHeader(id, position, rotation);
        NBT = nbt ?? new();
    }

    public EntityData DeepCopy()
    {
        return new EntityData(
            Header, NBT.DeepCopy()
        );
    }

    public override string ToString()
    {
        return Header.ToString();
    }
}
