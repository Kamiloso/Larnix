using System;
using System.Collections;
using System.Collections.Generic;
using Socket.Processing;

namespace Socket.Channel
{
    public class Packet
    {
        internal const int HEADER_SIZE = 2 + 1 + 8;

        internal CmdID ID = 0;
        internal byte Code = 0;
        internal long ControlSequence = 0; // it's basically (long)SeqNum (encrypted, for security)
        internal byte[] Bytes = null;

        internal Packet() { }
        internal Packet(CmdID id, byte code, byte[] bytes)
        {
            ID = id;
            Code = code;
            Bytes = bytes ?? new byte[0];
        }

        internal byte[] Serialize(Func<byte[], byte[]> encryption)
        {
            byte[] bytes = ArrayUtils.MegaConcat(
                EndianUnsafe.GetBytes(ID),
                EndianUnsafe.GetBytes(Code),
                EndianUnsafe.GetBytes(ControlSequence),
                Bytes ?? new byte[0]
                );

            return encryption == null ?
                bytes : encryption(bytes);
        }

        internal bool TryDeserialize(byte[] bytes, Func<byte[], byte[]> decryption)
        {
            if(decryption != null)
                bytes = decryption(bytes);

            if (bytes.Length < HEADER_SIZE)
                return false;

            ID = EndianUnsafe.FromBytes<CmdID>(bytes, 0);
            Code = bytes[2];
            ControlSequence = EndianUnsafe.FromBytes<long>(bytes, 3);
            Bytes = bytes[HEADER_SIZE..bytes.Length];

            return true;
        }
    }
}
