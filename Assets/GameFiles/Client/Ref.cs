using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Larnix.Client.Terrain;
using Larnix.Client.Entities;
using Larnix.Client.UI;
using Larnix.Physics;

namespace Larnix.Client
{
    public static class Ref // CLIENT GLOBAL REFERENCES (set in Awake())
    {
        // Mono Behaviours
        public static Client Client;
        public static MainPlayer MainPlayer;
        public static EntityProjections EntityProjections;
        public static LoadingScreen LoadingScreen;
        public static Loading Loading;
        public static GridManager GridManager;
        public static TileSelector TileSelector;
        public static Inventory Inventory;
        public static Screenshots Screenshots;
        public static Debug Debug;

        // Normal Classes
        public static PhysicsManager PhysicsManager;
    }
}
