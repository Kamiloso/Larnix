using System.Collections;
using System.Collections.Generic;
using System;
using Larnix.Core.References;
using Larnix.Server.Entities;
using Larnix.Server.Data;
using Larnix.Server.Terrain;
using Larnix.Socket.Backend;

namespace Larnix.Server
{
    internal abstract class Singleton : RefObject<Server>
    {
        protected Singleton(Server root) : base(root) {}
        protected Singleton(RefObject<Server> reff) : base(reff) {}

        public virtual void EarlyFrameUpdate() {}
        public virtual void PostEarlyFrameUpdate() {}
        public virtual void FrameUpdate() {}
        public virtual void LateFrameUpdate() {}
        public virtual void PostLateFrameUpdate() {}
    }
}
