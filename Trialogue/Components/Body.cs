using System.ComponentModel.DataAnnotations;
using BepuPhysics;
using BepuPhysics.Collidables;
using Trialogue.Ecs;

namespace Trialogue.Components
{
    public struct Body : IEcsComponent
    {
        public IShape Shape;

        public bool IsStatic;

        [Range(0, 1000)]
        public float Mass;

        public BodyReference Info;

        public void Dispose()
        {
            
        }
    }
}