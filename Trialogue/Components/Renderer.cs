using Trialogue.Ecs;
using Veldrid;

namespace Trialogue.Components;

public struct Renderer : IEcsComponent
{
    internal Pipeline PipeLine;

    public void Dispose()
    {
        PipeLine?.Dispose();
    }
}