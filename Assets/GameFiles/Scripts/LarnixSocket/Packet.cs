using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Larnix.Socket
{
    public class Packet
    {
        public const int MIN_SIZE = 2 * sizeof(byte);

        public byte ID = 0;
        public byte Code = 0;
        public byte[] Bytes = null;

        public Packet() { }
        public Packet(byte id, byte code, byte[] bytes)
        {
            ID = id;
            Code = code;
            Bytes = bytes;
        }

        public byte[] Serialize()
        {
            using (var ms = new MemoryStream())
            using (var writer = new BinaryWriter(ms))
            {
                writer.Write(ID);
                writer.Write(Code);

                if (Bytes != null)
                    writer.Write(Bytes);

                return ms.ToArray();
            }
        }

        public bool TryDeserialize(byte[] bytes)
        {
            if (bytes.Length < MIN_SIZE)
                return false;

            using (var ms = new MemoryStream(bytes))
            using (var reader = new BinaryReader(ms))
            {
                ID = reader.ReadByte();
                Code = reader.ReadByte();
                Bytes = reader.ReadBytes(bytes.Length - MIN_SIZE);
            }

            return true;
        }
    }
}
