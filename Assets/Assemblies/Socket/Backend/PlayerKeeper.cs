using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Larnix.Socket
{
    public class PlayerKeeper
    {
        public readonly ushort MaxPlayers;

        public PlayerKeeper(ushort maxPlayers)
        {
            MaxPlayers = maxPlayers;
        }
    }
}
