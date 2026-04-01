#nullable enable

namespace Larnix.Core;

public interface ITickable
{
    public void Tick(float deltaTime);
}
