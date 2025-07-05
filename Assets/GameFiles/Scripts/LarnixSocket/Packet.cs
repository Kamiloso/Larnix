using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Larnix.Socket
{
    public class Packet
    {
        public const int MIN_SIZE = 2 * sizeof(byte) + 1 * sizeof(uint);

        public byte ID = 0;
        public byte Code = 0;
        public uint ControlSequence = 0; // 0 by default, change if needed
        public byte[] Bytes = null;

        public Packet() { }
        public Packet(byte id, byte code, byte[] bytes)
        {
            ID = id;
            Code = code;
            Bytes = bytes;
        }

        public byte[] Serialize(Encryption.Settings encryption = null)
        {
            using (var ms = new MemoryStream())
            using (var writer = new BinaryWriter(ms))
            {
                writer.Write(ID);
                writer.Write(Code);
                writer.Write(ControlSequence);

                if (Bytes != null)
                    writer.Write(Bytes);

                byte[] bytes = ms.ToArray();
                if (encryption != null)
                    bytes = encryption.Encrypt(bytes);
                return bytes;
            }
        }

        public bool TryDeserialize(byte[] bytes, Encryption.Settings decryption = null)
        {
            if(decryption != null)
                bytes = decryption.Decrypt(bytes);

            if (bytes.Length < MIN_SIZE)
                return false;

            using (var ms = new MemoryStream(bytes))
            using (var reader = new BinaryReader(ms))
            {
                ID = reader.ReadByte();
                Code = reader.ReadByte();
                ControlSequence = reader.ReadUInt32();
                Bytes = reader.ReadBytes(bytes.Length - MIN_SIZE);
            }

            return true;
        }
    }
}
