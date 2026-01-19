using System.Collections;
using System.Collections.Generic;
using System;

namespace Larnix.Server.References
{
    internal abstract class ServerSingleton : RefObject
    {
        protected ServerSingleton(Server server) : base(server) {}

        public virtual void EarlyFrameUpdate() {}
        public virtual void PostEarlyFrameUpdate() {}
        public virtual void FrameUpdate() {}
        public virtual void LateFrameUpdate() {}
        public virtual void PostLateFrameUpdate() {}
    }
}
