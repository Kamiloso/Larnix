using System.Collections;
using System.Collections.Generic;
using Larnix.Client.Terrain;
using Larnix.Client.Entities;
using Larnix.Client.UI;
using Larnix.Core.Physics;
using Larnix.Client;
using Larnix.Menu.Worlds;
using Larnix.Client.Particles;
using Larnix.Background;
using Larnix.Core;

namespace Larnix
{
    public static class Ref // CLIENT GLOBAL AND MENU REFERENCES (set in Awake())
    {
        private static T Get<T>() where T : class => GlobRef.Get<T>();

        // Mono Behaviours (client)
        public static Client.Client Client => Get<Client.Client>();
        public static MainPlayer MainPlayer => Get<MainPlayer>();
        public static EntityProjections EntityProjections => Get<EntityProjections>();
        public static ParticleManager ParticleManager => Get<ParticleManager>();
        public static GridManager GridManager => Get<GridManager>();
        public static TileSelector TileSelector => Get<TileSelector>();
        public static Inventory Inventory => Get<Inventory>();
        public static Chat Chat => Get<Chat>();
        public static LoadingScreen LoadingScreen => Get<LoadingScreen>();
        public static Loading Loading => Get<Loading>();
        public static Screenshots Screenshots => Get<Screenshots>();
        public static Debugger Debugger => Get<Debugger>();

        // Normal Classes (client)
        public static PhysicsManager PhysicsManager => Get<PhysicsManager>();

        // Mono Behaviours (menu)
        public static Menu.Menu Menu => Get<Menu.Menu>();
        public static ServerSelect ServerSelect => Get<ServerSelect>();
        public static WorldSelect WorldSelect => Get<WorldSelect>();
        public static BasicGridManager BasicGridManager => Get<BasicGridManager>();

        // Universal references (client and menu)
        public static Sky Sky => Get<Sky>();
    }
}
