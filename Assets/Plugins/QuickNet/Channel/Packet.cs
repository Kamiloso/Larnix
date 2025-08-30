using System.Collections;
using System.Collections.Generic;
using QuickNet.Commands;
using QuickNet.Processing;

namespace QuickNet.Channel
{
    public class Packet
    {
        public const int MIN_SIZE = 1 * sizeof(CmdID) + 1 * sizeof(byte) + 1 * sizeof(uint);

        public CmdID ID = 0;
        public byte Code = 0;
        public uint ControlSequence = 0; // 0 by default, change if needed
        public byte[] Bytes = null;

        public Packet() { }
        public Packet(CmdID id, byte code, byte[] bytes)
        {
            ID = id;
            Code = code;
            Bytes = bytes;
        }

        public byte[] Serialize(Encryption.Settings encryption = null)
        {
            byte[] bytes = ArrayUtils.MegaConcat(
                EndianUnsafe.GetBytes(ID),
                EndianUnsafe.GetBytes(Code),
                EndianUnsafe.GetBytes(ControlSequence),
                Bytes ?? new byte[0]
                );

            return encryption?.Encrypt(bytes) ?? bytes;
        }

        public bool TryDeserialize(byte[] bytes, Encryption.Settings decryption = null)
        {
            if(decryption != null)
                bytes = decryption.Decrypt(bytes);

            if (bytes.Length < MIN_SIZE)
                return false;

            ID = (CmdID)bytes[0];
            Code = bytes[1];
            ControlSequence = EndianUnsafe.FromBytes<uint>(bytes, 2);
            Bytes = bytes[MIN_SIZE..bytes.Length];

            return true;
        }
    }
}
