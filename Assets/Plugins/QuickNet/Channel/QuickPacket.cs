using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using QuickNet.Processing;

namespace QuickNet.Channel
{
    public enum PacketFlag : byte
    {
        SYN = 1 << 0, // start connection (client -> server)
        FIN = 1 << 1, // end connection
        FAS = 1 << 2, // fast message / raw acknowledgement
        RSA = 1 << 3, // encrypted RSA
        NCN = 1 << 4, // no connection
    }

    public class QuickPacket
    {
        // ==== DATA SEGMENT ====

        public const int HEADER_SIZE = (2 + 2) + 4 + 4 + 1;
        public const ushort PROTOCOL_VERSION = 2;
        public Encryption.Settings Encrypt = null;

        public uint SeqNum { get; private set; } = 0;
        public uint AckNum { get; private set; } = 0;
        public byte Flags { get; private set; } = 0;
        public Packet Packet { get; private set; } = null;

        public QuickPacket() { }
        public QuickPacket(uint seqNum, uint ackNum, byte flags, Packet payload)
        {
            SeqNum = seqNum;
            AckNum = ackNum;
            Flags = flags;
            Packet = payload;
        }

        public bool HasFlag(PacketFlag flag)
        {
            return (Flags & (byte)flag) != 0;
        }

        public byte[] Serialize()
        {
            byte[] bytes = ArrayUtils.MegaConcat(
                EndianUnsafe.GetBytes(PROTOCOL_VERSION),
                EndianUnsafe.GetBytes(SeqNum),
                EndianUnsafe.GetBytes(AckNum),
                EndianUnsafe.GetBytes(Flags),
                Packet?.Serialize(Encrypt) ?? new byte[0]
                );

            ushort checksum = BytesSum(bytes);
            byte[] checksumBytes = EndianUnsafe.GetBytes(checksum);

            return ArrayUtils.MegaConcat(checksumBytes, bytes);
        }

        public bool TryDeserialize(byte[] bytes, bool ignorePayload = false)
        {
            if (bytes.Length < HEADER_SIZE)
                return false;

            ushort ChecksumRead, ChecksumCalculated;

            ChecksumRead = EndianUnsafe.FromBytes<ushort>(bytes, 0);
            ChecksumCalculated = (ushort)(BytesSum(bytes) - BytesSum(bytes[0..2]));
            if (ChecksumRead != ChecksumCalculated)
                return false;

            ushort version = EndianUnsafe.FromBytes<ushort>(bytes, 2);
            if (version != PROTOCOL_VERSION)
                return false;

            SeqNum = EndianUnsafe.FromBytes<uint>(bytes, 4);
            AckNum = EndianUnsafe.FromBytes<uint>(bytes, 8);
            Flags = bytes[12];
            Packet = null;

            if (!ignorePayload)
            {
                Packet = new Packet();
                byte[] payload_bytes = bytes[HEADER_SIZE..];
                if (!Packet.TryDeserialize(payload_bytes, Encrypt))
                    Packet = null;
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
