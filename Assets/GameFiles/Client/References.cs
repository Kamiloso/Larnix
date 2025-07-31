using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Larnix.Client.Terrain;
using Larnix.Client.Entities;
using Larnix.Client.UI;

namespace Larnix.Client
{
    public static class References // CLIENT GLOBAL REFERENCES (set in Awake())
    {
        public static Client Client;
        public static MainPlayer MainPlayer;
        public static EntityProjections EntityProjections;
        public static LoadingScreen LoadingScreen;
        public static Loading Loading;
        public static GridManager GridManager;
        public static TileSelector TileSelector;
        public static Inventory Inventory;
    }
}
