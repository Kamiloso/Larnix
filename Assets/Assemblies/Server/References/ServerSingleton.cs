using System.Collections;
using System.Collections.Generic;
using System;

namespace Larnix.Server.References
{
    public abstract class ServerSingleton : RefObject
    {
        protected ServerSingleton(Server server) : base(server) {}

        public virtual void EarlyFrameUpdate() {}
        public virtual void TechEarlyFrameUpdate() {}
        public virtual void FrameUpdate() {}
        public virtual void TechLateFrameUpdate() {}
        public virtual void LateFrameUpdate() {}
    }
}
