using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Security.Cryptography;

namespace Larnix.Server.Data
{
    public class Config
    {
        /*
        Default remote server values are default class parameters.
        Local server values are constructed by modifying default values with a special method.
        Remote server can store its values in JSON file and load them on start.
        */

        public ushort MaxPlayers { get; private set; } = 10;
        public ushort Port { get; private set; } = 27682;
        public bool AllowRemoteClients { get; private set; } = true;

        public RSA CompleteRSA { get; private set; } = null;

        public Config()
        {
            if(WorldLoad.LoadType == WorldLoad.LoadTypes.Local)
            {
                MaxPlayers = 1;
                Port = 0;
                AllowRemoteClients = false;
            }
            else if (WorldLoad.LoadType == WorldLoad.LoadTypes.Server)
            {
                CompleteRSA = new RSACryptoServiceProvider(2048);
            }
            else throw new System.Exception("Unknown server load type.");
        }

        public void Dispose()
        {
            if(CompleteRSA != null)
                CompleteRSA.Dispose();
        }
    }
}
