using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Larnix.Server
{
    public static class References // SERVER GLOBAL REFERENCES (set in Awake())
    {
        public static Server Server;
        public static EntityDataManager EntityDataManager;
        public static EntityManager EntityManager;
        public static ChunkLoading ChunkLoading;
        public static PlayerManager PlayerManager;
    }
}
