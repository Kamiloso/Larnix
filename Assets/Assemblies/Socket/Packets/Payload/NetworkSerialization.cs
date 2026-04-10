#nullable enable
using Larnix.Core.Serialization;
using Larnix.Core.Utils;
using Larnix.Socket.Security.Keys;

namespace Larnix.Socket.Packets.Payload;

internal static class NetworkSerialization
{
    private static int I0 => 0; // checksum
    private static int I1 => I0 + sizeof(ushort); // header
    private static int I2 => I1 + Binary<PayloadHeader>.Size; // encrypted contents

    public static byte[] ToBytes<T>(
        in PayloadHeader header, in T payload, IEncryptionKey key) where T : unmanaged
    {
        byte[] headerBytes = Binary<PayloadHeader>.Serialize(header);

        PayloadSafe<T> safePayload = new(header, new PayloadStruct<T>(payload));

        byte[] payloadSafe = Binary<PayloadSafe<T>>.Serialize(safePayload);
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

    public static bool TryDecryptNetworkBytes(byte[] bytes, IEncryptionKey key, out byte[]? decrypted)
    {
        decrypted = null;

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
        if (readSafe.Payload.CmdId != Cmd.Value<T>())
        {
            return false;
        }

        header = readSafe.Header;
        payload = readSafe.Payload.Contents;

        return true;
    }

    private static ushort CalculateChecksum(byte[] bytes)
    {
        ushort checksum = 0;
        for (int i = 0; i < bytes.Length; i++)
        {
            checksum += bytes[i];
        }
        return checksum;
    }
}
