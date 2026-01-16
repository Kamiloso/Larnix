using Larnix.Socket.Security.Keys;
using System.Collections;
using System.Collections.Generic;
using System;
using Larnix.Core.Serialization;
using Larnix.Core.Utils;

namespace Larnix.Socket.Structs
{
    internal enum PacketFlag : byte
    {
        SYN = 1 << 0, // start connection (client -> server)
        FIN = 1 << 1, // end connection
        FAS = 1 << 2, // fast message / raw acknowledgement
        RSA = 1 << 3, // encrypted RSA
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

        public static bool TryDeserialize(byte[] networkBytes, IEncryptionKey key, out PayloadBox output)
        {
            if (networkBytes.Length >= HEADER_SIZE)
            {
                PayloadBox box = new PayloadBox();

                ushort checksum = EndianUnsafe.FromBytes<ushort>(networkBytes, 0);
                ushort version = EndianUnsafe.FromBytes<ushort>(networkBytes, 2);

                if (version == PROTOCOL_VERSION && checksum == CheckSum(networkBytes, 2))
                {
                    box.SeqNum = EndianUnsafe.FromBytes<int>(networkBytes, 4);
                    box.AckNum = EndianUnsafe.FromBytes<int>(networkBytes, 8);
                    box.Flags = EndianUnsafe.FromBytes<byte>(networkBytes, 12);

                    if (key != null) // include contents
                    {
                        box.Bytes = key.Decrypt(networkBytes[13..]);

                        if (box.Bytes.Length >= Payload.BASE_HEADER_SIZE)
                        {
                            int secureSeq = EndianUnsafe.FromBytes<int>(box.Bytes, 3); // extract encrypted signature

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

        public static bool TryDeserializeOnlyHeader(byte[] networkBytes, out PayloadBox output)
        {
            return TryDeserialize(networkBytes, null, out output);
        }

        public byte[] Serialize(IEncryptionKey key)
        {
            byte[] serialized = ArrayUtils.MegaConcat(
                EndianUnsafe.GetBytes((ushort) 0),
                EndianUnsafe.GetBytes(PROTOCOL_VERSION),
                EndianUnsafe.GetBytes(SeqNum),
                EndianUnsafe.GetBytes(AckNum),
                EndianUnsafe.GetBytes(Flags),
                key.Encrypt(Bytes)
                );

            byte[] checksum = EndianUnsafe.GetBytes(CheckSum(serialized, 2));
            Buffer.BlockCopy(checksum, 0, serialized, 0, checksum.Length);

            return serialized;
        }

        private static ushort CheckSum(byte[] bytes, int from = 0)
        {
            unchecked
            {
                ushort sum = 0;
                for (int i = from; i < bytes.Length; i++)
                {
                    sum += bytes[i];
                }
                return sum;
            }
        }
    }
}
