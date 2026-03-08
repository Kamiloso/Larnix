using Larnix.Core.Vectors;
using Larnix.GameCore.Json;
using Larnix.GameCore.Enums;
using Larnix.GameCore;

namespace Larnix.Entities.Structs
{
    public class EntityData
    {
        public EntityHeader Header;
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

        public EntityData(EntityID id, Vec2 position, float rotation, Storage nbt)
        {
            Header = new EntityHeader(id, position, rotation);
            NBT = nbt;
        }

        public EntityData(EntityHeader header, Storage nbt)
        {
            Header = header;
            NBT = nbt;
        }

        public EntityData DeepCopy()
        {
            return new EntityData(
                Header, NBT.DeepCopy()
            );
        }
    }
}
