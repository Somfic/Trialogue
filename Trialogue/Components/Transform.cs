using System;
using System.Numerics;
using Trialogue.ECS;

namespace Trialogue.Components;

public struct Transform : IEcsComponent, IEcsAutoReset<Transform>
{
    public void AutoReset (ref Transform c) {
        c.Position = Vector3.Zero;
        c.Rotation = Vector3.Zero; 
        c.Scale = Vector3.One;
    }

    public Vector3 Position;
    
    public Vector3 Rotation;
    
    public Vector3 Scale;

    public void Dispose()
    {
    }
}