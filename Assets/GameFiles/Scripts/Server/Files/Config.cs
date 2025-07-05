using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Security.Cryptography;

public class Config
{
    public ushort MaxPlayers { get; private set; } = 10;
    public ushort Port { get; private set; } = 27682;
    public RSA CompleteRSA { get; set; } = new RSACryptoServiceProvider(2048);

    public void Dispose()
    {
        CompleteRSA.Dispose();
    }
}
