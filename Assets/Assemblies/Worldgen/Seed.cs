using System;
using Larnix.Core.Vectors;
using System.Buffers.Binary;
using System.Security.Cryptography;
using Larnix.Core.Utils;
using Larnix.Core.Binary;

namespace Larnix.Worldgen
{
    public class Seed
    {
        private readonly long _seed;

        public Seed(long seed)
        {
            _seed = seed;
        }

        public static explicit operator Seed(long value) => new Seed(value);
        public static implicit operator long(Seed seed) => seed._seed;

        public long Hash(string saltPhrase)
        {
            long salt = HashPhrase(saltPhrase);
            Span<byte> buffer = stackalloc byte[8 + 8];

            BinaryPrimitives.WriteInt64BigEndian(buffer.Slice(0, 8), _seed);
            BinaryPrimitives.WriteInt64BigEndian(buffer.Slice(8, 8), salt);

            using SHA256 sha = SHA256.Create();
            byte[] hash = sha.ComputeHash(buffer.ToArray());

            return BinaryPrimitives.ReadInt64BigEndian(hash.AsSpan(0, 8));
        }

        public int HashInt(string saltPhrase)
        {
            return (int)Hash(saltPhrase);
        }

        public long BlockHash(Vec2Int POS, string saltPhrase)
        {
            long salt = HashPhrase(saltPhrase);
            Span<byte> buffer = stackalloc byte[8 + 4 + 4 + 8];

            BinaryPrimitives.WriteInt64BigEndian(buffer.Slice(0, 8), _seed);
            BinaryPrimitives.WriteInt32BigEndian(buffer.Slice(8, 4), POS.x);
            BinaryPrimitives.WriteInt32BigEndian(buffer.Slice(12, 4), POS.y);
            BinaryPrimitives.WriteInt64BigEndian(buffer.Slice(16, 8), salt);

            using SHA256 sha = SHA256.Create();
            byte[] hash = sha.ComputeHash(buffer.ToArray());

            return BinaryPrimitives.ReadInt64BigEndian(hash.AsSpan(0, 8));
        }

        public int BlockHashInt(Vec2Int POS, string saltPhrase)
        {
            return (int)BlockHash(POS, saltPhrase);
        }

        private static long HashPhrase(string phrase)
        {
            if (string.IsNullOrEmpty(phrase)) throw new ArgumentException(nameof(phrase));

            using SHA256 sha = SHA256.Create();
            byte[] hash = sha.ComputeHash(Primitives.GetBytes((String64)phrase));

            return BinaryPrimitives.ReadInt64BigEndian(hash.AsSpan(0, 8));
        }
    }
}
