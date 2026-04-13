#nullable enable
using Larnix.Core.Serialization;
using Larnix.Core.Utils;
using Larnix.Socket.Security.Keys;

namespace Larnix.Socket.Payload;

internal static class NetworkSerializer
{
    private static int I0 => 0; // checksum
    private static int I1 => I0 + sizeof(ushort); // header
    private static int I2 => I1 + Binary<PayloadHeader>.Size; // encrypted contents

    public static byte[] ToBytes<T>(
        in PayloadHeader header, in T payload, IEncryptionKey? key) where T : unmanaged
    {
        key ??= KeyEmpty.Instance;

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

    public static bool TryDecryptNetworkBytes(byte[] bytes, IEncryptionKey? key, out byte[] decrypted)
    {
        key ??= KeyEmpty.Instance;

        decrypted = null!;

        if (bytes.Length < I2)
        {
            return false;
        }

        ushort checksum = Binary<ushort>.Deserialize(bytes, I0);
        if (checksum != CalculateChecksum(bytes) - CalculateChecksum(bytes[..2]))
        {
            return false;
        }

        PayloadHeader readHeader = Binary<PayloadHeader>.Deserialize(bytes, I1);
        if (!readHeader.CompatibleProtocolVersion())
        {
            return false;
        }

        byte[] decr = key.Decrypt(bytes[I2..]);
        if (decr.Length < Binary<PayloadHeader>.Size)
        {
            return false;
        }

        PayloadHeader encrHeader = Binary<PayloadHeader>.Deserialize(decr);
        if (encrHeader != readHeader)
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

        if (EndCompressor.SizeAfterDecompression(decrypted) != Binary<PayloadSafe<T>>.Size)
        {
            return false;
        }

        byte[] withNulls = EndCompressor.Decompress(decrypted);

        PayloadSafe<T> readSafe = Binary<PayloadSafe<T>>.Deserialize(withNulls);
        if (readSafe.Payload.CmdId != Cmd.Id<T>())
        {
            return false;
        }

        header = readSafe.Header;
        payload = readSafe.Payload.Contents;

        return true;
    }

    public static bool TryNetworkBytesAs<T>(
        byte[] bytes, IEncryptionKey? key, out PayloadHeader header, out T payload) where T : unmanaged
    {
        key ??= KeyEmpty.Instance;

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
            new PayloadHeader(),
            new PayloadStruct<T>(payload)
            );

        byte[] withNulls = Binary<PayloadSafe<T>>.Serialize(safe);
        return EndCompressor.Compress(withNulls);
    }

    private static ushort CalculateChecksum(byte[] bytes)
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
