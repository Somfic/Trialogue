using System;
using Trialogue.Ecs;
using Veldrid;

namespace Trialogue.Components
{
    public struct Renderer : IEcsComponent
    {
        internal Pipeline PipeLine;

        public void DrawUi(ref EcsEntity ecsEntity)
        {
            ecsEntity.Update(this);
        }
        
        public void Dispose()
        {
            PipeLine?.Dispose();
        }
    }
}