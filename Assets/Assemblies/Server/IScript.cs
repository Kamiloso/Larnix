using System.Collections;
using System.Collections.Generic;
using System;
using Larnix.Server.Entities;
using Larnix.Server.Data;
using Larnix.Server.Terrain;
using Larnix.Socket.Backend;

namespace Larnix.Server
{
    internal interface IScript
    {
        void EarlyFrameUpdate() {}
        void PostEarlyFrameUpdate() {}
        void FrameUpdate() {}
        void LateFrameUpdate() {}
        void PostLateFrameUpdate() {}
    }
}
