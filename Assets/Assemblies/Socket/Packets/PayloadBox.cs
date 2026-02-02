using System.Collections;
using System.Collections.Generic;
using System;
using Larnix.Core;
using Larnix.Core.Utils;
using Larnix.Core.Binary;

namespace Larnix.Socket.Packets
{
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

                ushort checksum = Primitives.FromBytes<ushort>(networkBytes, 0);
                ushort version = Primitives.FromBytes<ushort>(networkBytes, 2);

                if (version == PROTOCOL_VERSION && checksum == CheckSum(networkBytes, 2))
                {
                    box.SeqNum = Primitives.FromBytes<int>(networkBytes, 4);
                    box.AckNum = Primitives.FromBytes<int>(networkBytes, 8);
                    box.Flags = Primitives.FromBytes<byte>(networkBytes, 12);

                    if (key != null) // include contents
                    {
                        box.Bytes = key.Decrypt(networkBytes[13..]);

                        if (box.Bytes.Length >= Payload.BASE_HEADER_SIZE)
                        {
                            // extract encrypted signature
                            int secureSeq = Primitives.FromBytes<int>(box.Bytes, 3);

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
                Primitives.GetBytes((ushort) 0),
                Primitives.GetBytes(PROTOCOL_VERSION),
                Primitives.GetBytes(SeqNum),
                Primitives.GetBytes(AckNum),
                Primitives.GetBytes(Flags),
                key.Encrypt(Bytes)
                );

            byte[] checksum = Primitives.GetBytes(CheckSum(serialized, 2));
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
