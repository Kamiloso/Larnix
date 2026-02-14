using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Larnix.Core.Binary;

namespace Larnix.Core
{
    public struct Version : IBinary<Version>
    {
        public static readonly Version Current = new Version("0.0.33.2");

        public const int SIZE = sizeof(uint);
        public uint ID { get; private set; }

        public Version(uint id)
        {
            ID = id;
        }

        /// <summary>
        /// Examples: "1", "1.2", "1.2.3", "1.2.3.4". Fourth number doesn't affect compatibility.
        /// </summary>
        public Version(string str)
        {
            try
            {
                List<byte> segments = str.Split('.').Select(s => byte.Parse(s)).ToList();

                if (segments.Count > 4)
                    throw new Exception();

                while (segments.Count < 4)
                    segments.Add(0);

                uint constructID = 0;
                foreach (byte b in segments)
                {
                    constructID <<= 8;
                    constructID |= b;
                }
                ID = constructID;
            }
            catch
            {
                throw new ArgumentException($"Version {str} is invalid!");
            }
        }

        public bool CompatibleWith(Version version)
        {
            return ID >> 8 == version.ID >> 8;
        }

        public byte[] Serialize()
        {
            return Primitives.GetBytes(ID);
        }

        public bool Deserialize(byte[] bytes, int offset = 0)
        {
            if (offset + SIZE > bytes.Length)
                return false;
            
            ID = Primitives.FromBytes<uint>(bytes, offset);
            return true;
        }

        public static bool operator <(Version a, Version b) => a.ID < b.ID;
        public static bool operator >(Version a, Version b) => a.ID > b.ID;
        public static bool operator <=(Version a, Version b) => a.ID <= b.ID;
        public static bool operator >=(Version a, Version b) => a.ID >= b.ID;

        public override string ToString()
        {
            List<byte> segments = new()
            {
                (byte)((0xFF_00_00_00 & ID) >> 24),
                (byte)((0x00_FF_00_00 & ID) >> 16),
                (byte)((0x00_00_FF_00 & ID) >> 8),
                (byte)((0x00_00_00_FF & ID) >> 0),
            };

            while (segments.Count > 2 && segments[segments.Count - 1] == 0)
                segments.RemoveAt(segments.Count - 1);

            return string.Join(".", segments);
        }
    }
}
