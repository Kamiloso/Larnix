using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.ConstrainedExecution;
using System.Security.Cryptography;
using Larnix.Server.Data;
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

            string interface_str = null;

            int p1 = address.IndexOf('%');
            if (p1 != -1)
            {
                int p2 = p1 + 1;
                while (p2 < address.Length && char.IsDigit(address[p2])) p2++;

                interface_str = address.Substring(p1 + 1, p2 - p1 - 1);
                address = address.Remove(p1, p2 - p1);
            }

            try
            {
                var uri = new Uri($"udp://{address}");
                var ipAddresses = Dns.GetHostAddresses(uri.Host);
                if(interface_str != null) ipAddresses[0].ScopeId = int.Parse(interface_str);
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

        public static A_LoginTry tryLogin(string address, byte[] public_key, string nickname, string password, long serverSecret, long challengeID)
        {
            P_LoginTry prompt = new P_LoginTry(nickname, password, serverSecret, challengeID); // P_LoginTry doesn't accept challengeID == 0
            if (prompt.HasProblems)
                return null;

            using RSA rsa = RSA.Create();
            rsa.ImportParameters(new RSAParameters
            {
                Modulus = public_key[0..256],
                Exponent = public_key[256..],
            });

            Prompter prompter = WaitForPrompter(new Prompter(address, prompt.GetPacket(), rsa));
            prompter.Clean();

            if (prompter.State == Prompter.PrompterState.Ready)
            {
                Packet packet = prompter.AnswerPacket;
                if (packet == null || packet.ID != (byte)Name.A_LoginTry)
                    return null;

                A_LoginTry answer = new A_LoginTry(packet);
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
