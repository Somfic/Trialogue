using Trialogue.ECS;

namespace Trialogue.Components;

public struct Mesh : IEcsComponent
{
    public float[] Vertices { get; set; }
    
    public uint[] Indices { get; set; }
    internal uint VAO { get; set; }
    
    internal uint VBO { get; set; }
    internal uint EBO { get; set; }
    internal bool IsCompiled { get; set; }

    public void Dispose()
    {
        
    }
}