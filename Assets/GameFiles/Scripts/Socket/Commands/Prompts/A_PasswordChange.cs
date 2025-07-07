using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;
using UnityEngine;
using System.Linq;

namespace Larnix.Socket.Commands
{
    public class A_PasswordChange : BaseCommand
    {
        public override Name ID => Name.A_PasswordChange;
        public const int SIZE = 1;

        public ResultType Result { get; private set; } // 1B
        public enum ResultType : byte
        {
            Success,
            WrongUser,
            WrongPassword
        }

        public A_PasswordChange(ResultType result, byte code = 0)
            : base(Name.None, code)
        {
            Result = result;

            DetectDataProblems();
        }

        public A_PasswordChange(Packet packet)
            : base(packet)
        {
            byte[] bytes = packet.Bytes;
            if(bytes == null || bytes.Length != SIZE) {
                HasProblems = true;
                return;
            }

            Result = (ResultType)bytes[0];

            DetectDataProblems();
        }

        public override Packet GetPacket()
        {
            byte[] bytes = new byte[] { (byte)Result };
            return new Packet((byte)ID, Code, bytes);
        }
        protected override void DetectDataProblems()
        {
            bool ok = (
                Enum.IsDefined(typeof(ResultType), Result)
                );
            HasProblems = HasProblems || !ok;
        }
    }
}
