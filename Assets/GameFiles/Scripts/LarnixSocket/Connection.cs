using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using UnityEngine;
using UnityEditor.Sprites;

namespace Larnix.Socket
{
    public class Connection
    {
        private UdpClient Udp = null;
        public IPEndPoint EndPoint { get; private set; } = null;

        private uint SeqNum = 0; // last sent message ID
        private uint AckNum = 0; // last acknowledged message ID
        private uint GetNum = 0; // last received message ID

        public const uint MAX_RETRANSMISSIONS = 5;
        public const uint MAX_STAYING_PACKETS = 16384;
        public bool IsDead { get; private set; } = false;

        public const float ACK_CYCLE_TIME = 0.1f;
        private float currentAckCycleTime = 0.0f;

        private List<SafePacket> sendingPackets = new List<SafePacket>();
        private List<SafePacket> receivedPackets = new List<SafePacket>();
        private Queue<Packet> downloadablePackets = new Queue<Packet>();

        public Connection(UdpClient udp, IPEndPoint endPoint, Packet synPacket = null)
        {
            Udp = udp;
            EndPoint = endPoint;

            if (synPacket != null)
            {
                // Send SYN packet
                SendSafePacket(new SafePacket(
                    ++SeqNum,
                    GetNum,
                    (byte)SafePacket.PacketFlag.SYN,
                    synPacket
                    ));
            }
        }

        public void Tick(float deltaTime)
        {
            // If too many received packets, kill session
            if (receivedPackets.Count > MAX_STAYING_PACKETS)
            {
                IsDead = true;
                return;
            }

            // Make sequential queue out of received packets
            while (true)
            {
                bool foundNextPacket = false;
                for (int i = 0; i < receivedPackets.Count; i++)
                {
                    SafePacket safePacket = receivedPackets[i];
                    if (safePacket.SeqNum == GetNum + 1)
                    {
                        foundNextPacket = true;
                        if (safePacket.Payload != null)
                            downloadablePackets.Enqueue(safePacket.Payload);
                        break;
                    }
                }
                if (foundNextPacket) GetNum++;
                else break;
            }

            // Delete packets that are no longer needed
            receivedPackets.RemoveAll(p => p.SeqNum <= GetNum);

            // Check for retransmissions
            for (int i = 0; i < sendingPackets.Count; i++)
            {
                SafePacket safePacket = sendingPackets[i];
                safePacket.ReduceTime(deltaTime);
                if (safePacket.TimeToRetransmission == 0f)
                {
                    if (safePacket.SeqNum <= AckNum)
                    {
                        sendingPackets.RemoveAt(i--);
                    }
                    else
                    {
                        if (safePacket.RetransmissionCount < MAX_RETRANSMISSIONS)
                        {
                            Transmit(safePacket);
                            safePacket.Retransmited();
                        }
                        else
                        {
                            IsDead = true;
                            return;
                        }
                    }
                }
            }

            // Acknowledgements send
            currentAckCycleTime += deltaTime;
            bool ackSent = false;
            while (currentAckCycleTime >= ACK_CYCLE_TIME)
            {
                if (!ackSent)
                {
                    Send(null, false); // ACK packet (empty packet using fast mode)
                    ackSent = true;
                }
                currentAckCycleTime -= ACK_CYCLE_TIME;
            }
        }

        public void Send(Packet packet, bool safemode = true)
        {
            if (safemode)
            {
                SendSafePacket(new SafePacket(
                    ++SeqNum,
                    GetNum,
                    0,
                    packet
                    ));
            }
            else
            {
                Transmit(new SafePacket(
                    0,
                    GetNum,
                    (byte)SafePacket.PacketFlag.FAS,
                    packet
                    ));
            }
        }

        public Queue<Packet> Receive()
        {
            Queue<Packet> readyToRead = downloadablePackets;
            downloadablePackets = new Queue<Packet>();
            return readyToRead;
        }

        public void PushFromWeb(byte[] bytes)
        {
            SafePacket safePacket = new SafePacket();
            if (!safePacket.TryDeserialize(bytes))
                return;

            if (!safePacket.HasFlag(SafePacket.PacketFlag.FAS)) // safe mode
            {
                if (safePacket.SeqNum <= GetNum)
                    return;

                if (receivedPackets.Any(p => p.SeqNum == safePacket.SeqNum))
                    return;

                receivedPackets.Add(safePacket);
            }
            else // fast mode
            {
                if (safePacket.Payload != null)
                    downloadablePackets.Enqueue(safePacket.Payload);
            }

            // best received acknowledgement get
            if (safePacket.AckNum > AckNum)
                AckNum = safePacket.AckNum;

            // end connection
            if (safePacket.HasFlag(SafePacket.PacketFlag.FIN))
                IsDead = true;
        }

        public void KillConnection()
        {
            // Send 3 FIN flags (to ensure they arrive).
            // If they don't, protocol will automatically disconnect after a few seconds.

            SafePacket safePacket = new SafePacket(
                0,
                GetNum,
                (byte)SafePacket.PacketFlag.FAS | (byte)SafePacket.PacketFlag.FIN,
                null
                );

            const int FIN_COUNT = 3;
            for (int i = 0; i < FIN_COUNT; i++)
                Transmit(safePacket);

            IsDead = true;
        }
        private void SendSafePacket(SafePacket safePacket)
        {
            UnityEngine.Debug.Log("Send [S" + SeqNum + ", A" + AckNum + ", G" + GetNum + "] " + (safePacket.Payload != null ? safePacket.Payload.ID : "NULL"));

            Transmit(safePacket);
            sendingPackets.Add(safePacket);
        }

        private void Transmit(SafePacket safePacket)
        {
            if (!safePacket.HasFlag(SafePacket.PacketFlag.FAS))
                UnityEngine.Debug.Log("Transmit Seq" + safePacket.SeqNum);

            byte[] payload = safePacket.Serialize();
            Udp.Send(payload, payload.Length, EndPoint);
        }
    }
}
