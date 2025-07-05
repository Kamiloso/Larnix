using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Larnix.Socket
{
    public class SafePacket
    {
        // ==== DATA SEGMENT ====

        public const int HEADER_SIZE = 3 * sizeof(uint) + 1 * sizeof(byte) + 1 * sizeof(ushort);
        public const uint VERSION = 0;
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
            using (var ms = new MemoryStream())
            using (var writer = new BinaryWriter(ms))
            {
                writer.Write(VERSION);
                writer.Write(SeqNum);
                writer.Write(AckNum);
                writer.Write(Flags);

                byte[] bytes1 = ms.ToArray();
                byte[] bytes2 = Payload != null ? Payload.Serialize(Encrypt) : new byte[0];

                ushort checksum = (ushort)(BytesSum(bytes1) + BytesSum(bytes2));
                byte[] checksumBytes = BitConverter.GetBytes(checksum);

                return checksumBytes.Concat(bytes1).Concat(bytes2).ToArray();
            }
        }

        public bool TryDeserialize(byte[] bytes, bool ignorePayload = false)
        {
            if (bytes.Length < HEADER_SIZE)
                return false;

            ushort ChecksumRead, ChecksumCalculated;

            using (var ms = new MemoryStream(bytes))
            using (var reader = new BinaryReader(ms))
            {
                ChecksumRead = reader.ReadUInt16();
                ChecksumCalculated = BytesSum(bytes[2..]);
                if (ChecksumRead != ChecksumCalculated)
                    return false;

                uint version = reader.ReadUInt32();
                if (version != VERSION)
                    return false;

                SeqNum = reader.ReadUInt32();
                AckNum = reader.ReadUInt32();
                Flags = reader.ReadByte();
            }

            byte[] payload_bytes = bytes[HEADER_SIZE..];

            Payload = new Packet();
            if (ignorePayload || !Payload.TryDeserialize(payload_bytes, Encrypt))
                Payload = null;

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

        public const float RETRY_TIME = 0.6f;
        public float TimeToRetransmission { get; private set; } = RETRY_TIME;
        public uint RetransmissionCount { get; private set; } = 0;

        public void ReduceTime(float time)
        {
            TimeToRetransmission -= time;
            if (TimeToRetransmission < 0f)
                TimeToRetransmission = 0f;
        }
        public void Retransmited()
        {
            TimeToRetransmission = RETRY_TIME;
            RetransmissionCount++;
        }
    }
}
