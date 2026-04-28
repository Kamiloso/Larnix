#nullable enable
using System.Net;

namespace Larnix.Socket.Helpers;

internal static class WebIdentity
{
    public static string GetCIDR(IPAddress address, int subnet)
    {
        byte[] addrBytes = address.GetAddressBytes();
        MaskBytes(addrBytes, ref subnet);
        return $"{new IPAddress(addrBytes)}/{subnet}";
    }

    private static void MaskBytes(byte[] bytes, ref int subnet)
    {
        if (subnet < 0) subnet = 0;
        if (subnet > bytes.Length * 8) subnet = bytes.Length * 8;

        int totalBytes = bytes.Length;
        int fullBytes = subnet / 8;
        int remainingBits = subnet % 8;

        if (fullBytes == totalBytes) return;

        bool hasRemainingBits = remainingBits > 0;

        byte remainingByte = 0;
        if (hasRemainingBits)
        {
            byte remaining_mask = (byte)(0xFF << (8 - remainingBits));
            remainingByte = (byte)(remaining_mask & bytes[fullBytes]);
        }

        if (hasRemainingBits)
        {
            bytes[fullBytes] = remainingByte;
        }

        int zeroingFrom = hasRemainingBits ? fullBytes + 1 : fullBytes;
        for (int i = zeroingFrom; i < totalBytes; i++)
        {
            bytes[i] = 0;
        }
    }
}
