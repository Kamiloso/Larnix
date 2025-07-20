using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Larnix.Socket.Commands
{
    public abstract class BaseCommand
    {
        public bool HasProblems { get; protected set; } = false;
        public abstract Name ID { get; }
        public byte Code { get; private set; } = 0;

        public BaseCommand(Name id, byte code)
        {
            HasProblems = HasProblems || (id != Name.None && id != ID);
            Code = code;
        }
        public BaseCommand(Packet packet)
        {
            HasProblems = HasProblems || ((Name)packet.ID != Name.None && (Name)packet.ID != ID);
            Code = packet.Code;
        }
        public abstract Packet GetPacket();
        protected abstract void DetectDataProblems();
    }
}
