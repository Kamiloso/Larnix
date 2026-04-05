#nullable enable
using Larnix.Core;
using Larnix.Core.Vectors;
using Larnix.Model.Entities.Structs;
using Larnix.Server.Packets;
using static Larnix.Server.Packets.CodeInfo;

namespace Larnix.Server.Entities.Controllers;

internal class PlayerController : BaseController
{
    public string Nickname { get; }

    private IServer Server => GlobRef.Get<IServer>();
    private IClock Clock => GlobRef.Get<IClock>();

    public PlayerController(ulong uid, string nickname, EntityData entityData) : base(uid, entityData)
    {
        Nickname = nickname;

        PlayerInitialize packet = new(Position, Uid, Clock.FixedFrame);
        Server.Send(Nickname, packet);
    }

    public void UpdateTransform(PlayerUpdate msg)
    {
        if (!IsActive)
        {
            Activate();
        }

        RealInstance!.SetTransform(msg.Position, msg.Rotation);
    }

    public void AcceptTeleport(Vec2 newPosition)
    {
        ; // do nothing by now
    }

    protected override void OnKill()
    {
        CodeInfo packet = new(Info.YouDie);
        Server.Send(Nickname, packet);
    }
}
