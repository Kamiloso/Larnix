using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Larnix.Socket
{
    public class PacketAndOwner
    {
        public string Nickname { get; set; }
        public Packet Packet { get; set; }

        public PacketAndOwner(string _nickname, Packet _packet)
        {
            Nickname = _nickname;
            Packet = _packet;
        }
    }
}
