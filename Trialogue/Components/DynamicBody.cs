using System.ComponentModel.DataAnnotations;
using BepuPhysics;
using BepuPhysics.Collidables;
using Trialogue.Ecs;

namespace Trialogue.Components;

public struct DynamicBody : IEcsComponent
{
    public IShape Shape;
        
    [Range(0, 1000)]
    public float Mass;

    internal BodyHandle Handle;

    public void Dispose()
    {
            
    }
}
    
public struct Static : IEcsComponent
{
    public IShape Shape;

    internal StaticHandle Handle;

    public void Dispose()
    {
            
    }
}