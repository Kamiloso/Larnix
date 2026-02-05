using System.Collections;
using System.Collections.Generic;
using Larnix.Client.Terrain;
using Larnix.Client.Entities;
using Larnix.Client.UI;
using Larnix.Core.Physics;
using Larnix.Client;
using Larnix.Menu.Worlds;

namespace Larnix
{
    public static class Ref // CLIENT GLOBAL AND MENU REFERENCES (set in Awake())
    {
        // Mono Behaviours (client)
        public static Client.Client Client;
        public static MainPlayer MainPlayer;
        public static EntityProjections EntityProjections;
        public static ParticleManager ParticleManager;
        public static LoadingScreen LoadingScreen;
        public static Loading Loading;
        public static TileSelector TileSelector;
        public static Inventory Inventory;
        public static Screenshots Screenshots;
        public static Debugger Debugger;

        // Mono Behaviours (menu)
        public static Menu.Menu Menu;
        public static ServerSelect ServerSelect;
        public static WorldSelect WorldSelect;

        // Mono Behaviours (both)
        public static GridManager GridManager;

        // Normal Classes
        public static PhysicsManager PhysicsManager;
    }
}
