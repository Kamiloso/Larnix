#nullable enable
using Larnix.Core.Serialization;
using Larnix.Core.Utils;
using Larnix.Socket.Payload;
using System;
using Larnix.Socket.Security.Encryption;

namespace Larnix.Socket.Session.Tools;

internal static class NetworkSerializer
{
    private static int I1 => Binary<ushort>.Size; // plain header
    private static int I2 => I1 + Binary<PayloadHeader>.Size; // encrypted contents

    public static byte[] ToBytes<T>(
        in PayloadHeader header, in T payload, IEncryptor? key) where T : unmanaged
    {
        key ??= EmptyKey.Instance;

        byte[] headerBytes = Binary<PayloadHeader>.Serialize(header);

        PayloadStruct<T> pstruct = new(payload);
        PayloadSafe<T> safe = new(header, pstruct);

        byte[] payloadSafe = Binary<PayloadSafe<T>>.Serialize(safe);
        byte[] nullsTrimmed = EndCompressor.Compress(payloadSafe);
        byte[] payloadBytes = key.Encrypt(nullsTrimmed);

        ushort checksum = (ushort)(CalculateChecksum(headerBytes) + CalculateChecksum(payloadBytes));
        byte[] checksumBytes = Binary<ushort>.Serialize(checksum);

        return ArrayUtils.MegaConcat(checksumBytes, headerBytes, payloadBytes);
    }

    public static bool TryPlainHeaderFromBytes(byte[] bytes, out PayloadHeader header)
    {
        if (bytes.Length < I2)
        {
            header = default;
            return false;
        }

        header = Binary<PayloadHeader>.Deserialize(bytes, I1);
        return true;
    }

    public static bool TryDecryptNetworkBytes(byte[] bytes, IDecryptor? key, out byte[] decrypted)
    {
        key ??= EmptyKey.Instance;

        decrypted = null!;

        if (bytes.Length < I2)
        {
            return false;
        }

        ushort checksum = Binary<ushort>.Deserialize(bytes);
        if (checksum != CalculateChecksum(bytes.AsSpan(2..)))
        {
            return false;
        }

        PayloadHeader plainHeader = Binary<PayloadHeader>.Deserialize(bytes, I1);
        if (!plainHeader.CompatibleProtocolVersion())
        {
            return false;
        }

        byte[] decr = key.Decrypt(bytes[I2..]);
        if (EndCompressor.SizeAfterDecompression(decr) < Binary<PayloadHeader>.Size)
        {
            return false;
        }

        byte[] headerBytes = EndCompressor.PartialDecompress(decr, 0, Binary<PayloadHeader>.Size);
        PayloadHeader encrHeader = Binary<PayloadHeader>.Deserialize(headerBytes);
        if (encrHeader != plainHeader)
        {
            return false;
        }

        decrypted = decr;
        return true;
    }

    public static bool TryDecryptedBytesAs<T>(
        byte[] decrypted, out PayloadHeader header, out T payload) where T : unmanaged
    {
        header = default;
        payload = default;

        if (EndCompressor.SizeAfterDecompression(decrypted) != Binary<PayloadSafe<T>>.Size ||
            CmdIdFromDecryptedBytesFast(decrypted) != Cmd.Id<T>())
        {
            return false;
        }

        byte[] withNulls = EndCompressor.Decompress(decrypted);
        PayloadSafe<T> readSafe = Binary<PayloadSafe<T>>.Deserialize(withNulls);

        header = readSafe.Header;
        payload = readSafe.Payload.Contents;

        return true;
    }

    public static bool TryNetworkBytesAs<T>(
        byte[] bytes, IDecryptor? key, out PayloadHeader header, out T payload) where T : unmanaged
    {
        key ??= EmptyKey.Instance;

        header = default;
        payload = default;

        if (!TryDecryptNetworkBytes(bytes, key, out byte[] decrypted)) return false;
        if (!TryDecryptedBytesAs(decrypted, out PayloadHeader header1, out T payload1)) return false;

        header = header1;
        payload = payload1;

        return true;
    }

    public static byte[] PackAsIfDecrypted<T>(in T payload) where T : unmanaged
    {
        var safe = new PayloadSafe<T>(
            new PayloadHeader(), // dummy header
            new PayloadStruct<T>(payload)
            );

        byte[] withNulls = Binary<PayloadSafe<T>>.Serialize(safe);
        return EndCompressor.Compress(withNulls);
    }

    private static short CmdIdFromDecryptedBytesFast(byte[] decrypted)
    {
        // WARNING: if too small to read cmdid, will return 0

        int I = Binary<PayloadHeader>.Size; // cmdid

        int plainLength = decrypted.Length > 2
            ? decrypted.Length - 2
            : decrypted.Length;

        byte byte1 = plainLength > I ? decrypted[I] : (byte)0;
        byte byte2 = plainLength > I + 1 ? decrypted[I + 1] : (byte)0;

        return BitConverter.IsLittleEndian // will probably always be true, but just in case
            ? (short)(byte2 << 8 | byte1)
            : (short)(byte1 << 8 | byte2);
    }

    private static ushort CalculateChecksum(Span<byte> bytes)
    {
        unchecked
        {
            ushort checksum = 0;
            for (int i = 0; i < bytes.Length; i++)
            {
                checksum += bytes[i];
            }
            return checksum;
        }
    }
}
