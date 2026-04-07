using System;
using Larnix.Socket.Security.Keys;
using Larnix.Core.Utils;
using Larnix.Core;

namespace Larnix.Socket.Packets;

internal enum PacketFlag : byte
{
    SYN = 1 << 0, // start connection (client -> server)
    FIN = 1 << 1, // end connection
    FAS = 1 << 2, // fast message / raw acknowledgement
    RSA = 1 << 3, // encrypted with RSA
    NCN = 1 << 4, // no connection
}

internal class PayloadBox
{
    private const int HEADER_SIZE = 2 + 2 + 4 + 4 + 1;
    private const ushort PROTOCOL_VERSION = 4;

    public int SeqNum { get; private set; } // [4..8]
    public int AckNum { get; private set; } // [8..12]
    public byte Flags { get; private set; } // [12..13]
    public byte[] Bytes { get; private set; } // [13..]

    private PayloadBox() { }
    public PayloadBox(int seqNum, int ackNum, byte flags, Payload payload)
    {
        SeqNum = seqNum;
        AckNum = ackNum;
        Flags = flags;
        Bytes = payload.Serialize(seqNum);
    }

    public bool HasFlag(PacketFlag flag)
    {
        return (Flags & (byte)flag) != 0;
    }

    public void SetFlag(PacketFlag flag)
    {
        Flags |= (byte)flag;
    }

    public void UnsetFlag(PacketFlag flag)
    {
        Flags &= (byte)~flag;
    }

    public static bool TryDeserialize(byte[] networkBytes, IEncryptionKey key, out PayloadBox output)
    {
        if (networkBytes.Length >= HEADER_SIZE)
        {
            PayloadBox box = new PayloadBox();

            ushort checksum = Binary<ushort>.Deserialize(networkBytes, 0);
            ushort version = Binary<ushort>.Deserialize(networkBytes, 2);

            if (version == PROTOCOL_VERSION && checksum == CheckSum(networkBytes, 2))
            {
                box.SeqNum = Binary<int>.Deserialize(networkBytes, 4);
                box.AckNum = Binary<int>.Deserialize(networkBytes, 8);
                box.Flags = Binary<byte>.Deserialize(networkBytes, 12);

                if (key != null) // include contents
                {
                    box.Bytes = key.Decrypt(networkBytes[13..]);

                    if (box.Bytes.Length >= Payload.BASE_HEADER_SIZE)
                    {
                        // extract encrypted signature
                        int secureSeq = Binary<int>.Deserialize(box.Bytes, 3);

                        // check integrity
                        if (secureSeq == box.SeqNum)
                        {
                            output = box;
                            return true;
                        }
                    }
                }
                else // ignore contents
                {
                    output = box;
                    return true;
                }
            }
        }

        output = null;
        return false;
    }

    public static bool TryDeserializeHeader(byte[] networkBytes, out PayloadBox output)
    {
        return TryDeserialize(networkBytes, null, out output);
    }

    public byte[] Serialize(IEncryptionKey key)
    {
        byte[] serialized = ArrayUtils.MegaConcat(
            Binary<ushort>.Serialize(0),
            Binary<ushort>.Serialize(PROTOCOL_VERSION),
            Binary<int>.Serialize(SeqNum),
            Binary<int>.Serialize(AckNum),
            Binary<byte>.Serialize(Flags),
            key.Encrypt(Bytes)
            );

        byte[] checksum = Binary<ushort>.Serialize(CheckSum(serialized, 2));
        Buffer.BlockCopy(checksum, 0, serialized, 0, checksum.Length);

        return serialized;
    }

    private static ushort CheckSum(byte[] bytes, int from = 0)
    {
        ushort sum = 0;
        for (int i = from; i < bytes.Length; i++)
        {
            sum += bytes[i];
        }
        return sum;
    }
}
