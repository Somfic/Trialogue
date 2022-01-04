using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Trialogue.Ecs;
using Trialogue.Structures.Shaders;
using Trialogue.Structures.Textures;

namespace Trialogue.Components;

public struct Material : IEcsComponent
{
    internal uint ShaderProgram;
    internal ShaderSource[] Shaders { get; private set; }
    internal TextureSource[] Textures { get; private set; }
    
    public bool IsCompiled { get; internal set; }
    public void AddShader(params ShaderSource[] shaders)
    {
        IsCompiled = false;
        
        Shaders ??= Array.Empty<ShaderSource>();
        
        var list = Shaders.ToList();
        list.AddRange(shaders);
        Shaders = list.ToArray();
    }
    
    public void AddTexture(params TextureSource[] textures)
    {
        IsCompiled = false;

        Textures ??= Array.Empty<TextureSource>();
        
        var list = Textures.ToList();
        list.AddRange(textures);
        Textures = list.ToArray();
    }

    public void Dispose()
    {
        
    }
}