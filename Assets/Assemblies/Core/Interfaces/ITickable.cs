using System;

namespace Larnix.Core.Interfaces;

public interface ITickable
{
    public void Tick(float deltaTime);
}
