#nullable enable
using System.Collections.Generic;
using Larnix.Server.Packets.Structs;
using Larnix.Core.Serialization;
using Larnix.Socket.Payload;
using System.Runtime.InteropServices;

namespace Larnix.Server.Packets;

[CmdId(13)]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct BlockUpdate : ISanitizable<BlockUpdate>
{
    public FixedBuffer1024<BlockUpdateRecord> BlockRecords { get; }

    private BlockUpdate(in FixedBuffer1024<BlockUpdateRecord> blockRecords)
    {
        BlockRecords = Sanitizer.Filter(blockRecords);
    }

    public static IEnumerable<BlockUpdate> CreateList(BlockUpdateRecord[] blockRecords)
    {
        int s1 = 0; // start
        int l1 = 0; // length
        do
        {
            s1 += l1;
            l1 = 0;

            FixedBuffer1024<BlockUpdateRecord> buffer = new();
            while (s1 + l1 < blockRecords.Length)
            {
                buffer.Add(blockRecords[s1 + l1++]);
            }

            yield return new BlockUpdate(buffer);

        } while (l1 > 0);
    }

    public BlockUpdate Sanitize()
    {
        return new BlockUpdate(BlockRecords);
    }
}
