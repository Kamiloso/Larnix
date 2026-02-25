using System;

namespace Larnix.Core
{
    public interface ITickable
    {
        public void Tick(float deltaTime);
    }
}
