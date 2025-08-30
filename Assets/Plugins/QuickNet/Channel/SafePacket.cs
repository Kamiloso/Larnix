using System;
using System.Collections;
using System.Collections.Generic;
using QuickNet.Processing;

namespace QuickNet.Channel
{
    public class SafePacket
    {
        // ==== DATA SEGMENT ====

        public const int HEADER_SIZE = 3 * sizeof(uint) + 1 * sizeof(byte) + 1 * sizeof(ushort);
        public const uint PROTOCOL_VERSION = 1;
        public Encryption.Settings Encrypt = null;

        public uint SeqNum { get; private set; } = 0;
        public uint AckNum { get; private set; } = 0;
        public byte Flags { get; private set; } = 0;
        public Packet Payload { get; private set; } = null;

        public SafePacket() { }
        public SafePacket(uint seqNum, uint ackNum, byte flags, Packet payload)
        {
            SeqNum = seqNum;
            AckNum = ackNum;
            Flags = flags;
            Payload = payload;
        }

        public enum PacketFlag : byte
        {
            SYN = 1 << 0, // start connection (client -> server)
            FIN = 1 << 1, // end connection
            FAS = 1 << 2, // fast message / raw acknowledgement
            RSA = 1 << 3, // encrypted RSA
            NCN = 1 << 4, // no connection
        }
        public bool HasFlag(PacketFlag flag)
        {
            return (Flags & (byte)flag) != 0;
        }

        public byte[] Serialize()
        {
            byte[] bytes1 = ArrayUtils.MegaConcat(
                EndianUnsafe.GetBytes(PROTOCOL_VERSION),
                EndianUnsafe.GetBytes(SeqNum),
                EndianUnsafe.GetBytes(AckNum),
                EndianUnsafe.GetBytes(Flags)
                );

            byte[] bytes2 = Payload?.Serialize(Encrypt) ?? new byte[0];

            ushort checksum = (ushort)(BytesSum(bytes1) + BytesSum(bytes2));
            byte[] checksumBytes = EndianUnsafe.GetBytes(checksum);

            return ArrayUtils.MegaConcat(checksumBytes, bytes1, bytes2);
        }

        public bool TryDeserialize(byte[] bytes, bool ignorePayload = false)
        {
            if (bytes.Length < HEADER_SIZE)
                return false;

            ushort ChecksumRead, ChecksumCalculated;

            ChecksumRead = EndianUnsafe.FromBytes<ushort>(bytes, 0);
            ChecksumCalculated = BytesSum(bytes[2..]);
            if (ChecksumRead != ChecksumCalculated)
                return false;

            uint version = EndianUnsafe.FromBytes<uint>(bytes, 2);
            if (version != PROTOCOL_VERSION)
                return false;

            SeqNum = EndianUnsafe.FromBytes<uint>(bytes, 6);
            AckNum = EndianUnsafe.FromBytes<uint>(bytes, 10);
            Flags = bytes[14];
            Payload = null;

            if (!ignorePayload)
            {
                Payload = new Packet();
                byte[] payload_bytes = bytes[HEADER_SIZE..];
                if (!Payload.TryDeserialize(payload_bytes, Encrypt))
                    Payload = null;
            }

            return true;
        }

        private static ushort BytesSum(byte[] bytes)
        {
            ushort sum = 0;
            foreach (byte b in bytes)
                sum += b;
            return sum;
        }

        // ==== RETRANSMISSION SEGMENT ====

        public float TimeToRetransmission { get; private set; } = float.MaxValue;
        public uint TransmissionCount { get; private set; } = 0;

        public void ReduceTime(float time)
        {
            TimeToRetransmission -= time;
            if (TimeToRetransmission < 0f)
                TimeToRetransmission = 0f;
        }
        public void Transmited(float retryTime)
        {
            TimeToRetransmission = retryTime;
            TransmissionCount++;
        }
    }
}
