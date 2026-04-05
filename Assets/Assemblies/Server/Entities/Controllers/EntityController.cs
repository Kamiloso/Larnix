#nullable enable
using Larnix.Model.Entities.Structs;

namespace Larnix.Server.Entities.Controllers;

internal class EntityController : BaseController
{
    public EntityController(ulong uid, EntityData entityData) : base(uid, entityData) { }
}
