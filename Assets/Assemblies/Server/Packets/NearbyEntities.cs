#nullable enable
using System.Collections.Generic;
using Larnix.Core.Serialization;
using System.Runtime.InteropServices;
using Larnix.Socket.Payload;

namespace Larnix.Server.Packets;

[CmdId(0x08)]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct NearbyEntities : ISanitizable<NearbyEntities>
{
    public uint FixedFrame { get; }
    public FixedBuffer512<ulong> AddEntities { get; }
    public FixedBuffer512<ulong> RemoveEntities { get; }

    private NearbyEntities(uint fixedFrame, in FixedBuffer512<ulong> addEntities, in FixedBuffer512<ulong> removeEntities)
    {
        FixedFrame = fixedFrame;
        AddEntities = addEntities;
        RemoveEntities = removeEntities;
    }

    public static NearbyEntities CreateBootstrap(uint fixedFrame)
    {
        return new NearbyEntities(
            fixedFrame,
            new FixedBuffer512<ulong>(),
            new FixedBuffer512<ulong>()
            );
    }

    public static IEnumerable<NearbyEntities> GenerateList(uint fixedFrame, ulong[] addEntities, ulong[] removeEntities)
    {
        int s1 = 0, s2 = 0; // start
        int l1 = 0, l2 = 0; // length
        do
        {
            (s1, s2) = (s1 + l1, s2 + l2);
            (l1, l2) = (0, 0);

            FixedBuffer512<ulong> addBuffer = new();
            while (s1 + l1 < addEntities.Length)
            {
                addBuffer.Add(addEntities[s1 + l1++]);
            }

            FixedBuffer512<ulong> removeBuffer = new();
            while (s2 + l2 < removeEntities.Length)
            {
                removeBuffer.Add(removeEntities[s2 + l2++]);
            }

            yield return new NearbyEntities(fixedFrame, addBuffer, removeBuffer);

        } while (l1 > 0 || l2 > 0);
    }

    public NearbyEntities Sanitize()
    {
        return new NearbyEntities(FixedFrame, AddEntities, RemoveEntities);
    }
}
