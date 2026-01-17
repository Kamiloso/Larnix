using UnityEngine;

namespace Larnix.Server.References
{
    public abstract class RefObject
    {
        protected readonly Server ThisServer;

        protected RefObject(Server server) => ThisServer = server;
        protected RefObject(RefObject reff) => ThisServer = reff.ThisServer;

        protected T Ref<T>() where T : class
        {
            return ThisServer.Ref<T>();
        }
    }
}
