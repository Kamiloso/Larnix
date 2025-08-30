using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using System.Security.Cryptography;
using QuickNet.Processing;
using QuickNet.Channel;

namespace QuickNet.Frontend
{
    public class Prompter
    {
        public PrompterState State = PrompterState.None;
        public Packet AnswerPacket = null;

        private UdpClient udpClient;
        private IPEndPoint endPoint = null;
        private uint PromptID = 0;

        private const float MAX_WAITING_TIME = 2f; // seconds
        private float waitingTime = 0f;

        public enum PrompterState : byte
        {
            None,
            Waiting,
            Ready,
            Timeout,
            Error
        }

        public Prompter(string ip_address, Packet prompt, RSA publicKeyRSA = null)
        {
            endPoint = Resolver.ResolveStringSync(ip_address);
            if(endPoint == null)
            {
                State = PrompterState.Error;
                AnswerPacket = null;
                return;
            }

            udpClient = QuickClient.CreateConfiguredClientObject(endPoint);
            PromptID = (uint)(int)KeyObtainer.GetSecureLong();

            SafePacket.PacketFlag flags = SafePacket.PacketFlag.NCN;
            Encryption.Settings encrypt = null;
            if(publicKeyRSA != null)
            {
                flags |= SafePacket.PacketFlag.RSA;
                encrypt = new Encryption.Settings(Encryption.Settings.Type.RSA, publicKeyRSA);
            }

            SafePacket safePacket = new SafePacket(
                seqNum: PromptID, // SeqNum - Prompt ID in this context
                ackNum: 0,
                flags: (byte)flags,
                payload: prompt
                );
            safePacket.Encrypt = encrypt;

            byte[] bytes = safePacket.Serialize();
            udpClient.SendAsync(bytes, bytes.Length, endPoint);

            State = PrompterState.Waiting;
        }

        public void Tick(float deltaTime)
        {
            waitingTime += deltaTime;
            if(waitingTime > MAX_WAITING_TIME)
            {
                State = PrompterState.Timeout;
                AnswerPacket = null;
                return;
            }

            while (udpClient.Available > 0)
            {
                IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
                byte[] bytes = null;

                try
                {
                    bytes = udpClient.Receive(ref remoteEP);
                }
                catch (SocketException ex)
                {
                    if (ex.SocketErrorCode == SocketError.WouldBlock)
                    {
                        break;
                    }
                    else if (ex.SocketErrorCode == SocketError.ConnectionReset)
                    {
                        State = PrompterState.Error;
                        AnswerPacket = null;
                        return;
                    }
                    else throw;
                }

                if (bytes == null)
                    continue;

                if (endPoint.Equals(remoteEP))
                {
                    SafePacket safePacket = new SafePacket();
                    if(safePacket.TryDeserialize(bytes))
                    {
                        if(safePacket.SeqNum == PromptID && safePacket.HasFlag(SafePacket.PacketFlag.NCN))
                        {
                            State = PrompterState.Ready;
                            AnswerPacket = safePacket.Payload;
                            return;
                        }
                    }
                }
            }
        }

        public IPEndPoint GetEndPoint()
        {
            return new IPEndPoint(endPoint.Address, endPoint.Port);
        }

        public void Clean()
        {
            if(udpClient != null)
                udpClient.Dispose();
        }
    }
}
