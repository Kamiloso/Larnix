using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using Larnix.Socket.Commands;

namespace Larnix.Socket
{
    public static class Resolver
    {
        public static IPEndPoint ResolveString(string address)
        {
            if (address.EndsWith(']') || !address.Contains(':'))
                address += ":27682";

            if (address.Count(c => c == ':') >= 2 && !address.StartsWith("[") && !address.EndsWith("]"))
                address = '[' + address + "]:27682";

            try
            {
                var uri = new Uri($"udp://{address}");
                var ipAddresses = Dns.GetHostAddresses(uri.Host);
                return new IPEndPoint(ipAddresses[0], uri.Port);
            }
            catch
            {
                return null;
            }
        }

        public static A_ServerInfo downloadServerInfo(string address, string nickname)
        {
            P_ServerInfo prompt = new P_ServerInfo(nickname);
            if (prompt.HasProblems)
                return null;

            Prompter prompter = WaitForPrompter(new Prompter(address, prompt.GetPacket()));
            prompter.Clean();

            if (prompter.State == Prompter.PrompterState.Ready)
            {
                Packet packet = prompter.AnswerPacket;
                if (packet == null || packet.ID != (byte)Name.A_ServerInfo)
                    return null;

                A_ServerInfo answer = new A_ServerInfo(packet);
                if (answer.HasProblems)
                    return null;

                return answer;
            }
            else return null;
        }

        private static Prompter WaitForPrompter(Prompter prompter)
        {
            while (true)
            {
                if (prompter.State != Prompter.PrompterState.Waiting)
                    break;

                System.Threading.Thread.Sleep(100);
                prompter.Tick(0.1f);
            }
            return prompter;
        }
    }
}
