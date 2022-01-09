using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Trialogue;
using Trialogue.Components;
using Trialogue.ECS;
using Trialogue.Structures.Shaders;
using Trialogue.Structures.Textures;
using Trialogue.Systems.Rendering;

public class Sandbox : TrialogueEngine
{
    private readonly ILogger<Sandbox>? _log;

    public Sandbox(IServiceProvider services) : base(services)
    {
        _log = services.GetService<ILogger<Sandbox>>();
    }

    protected override void OnSetup()
    {
        AddRenderSystem<MeshRenderer>();
        AddRenderSystem<WindowSystem>();

        var entity = CreateEntity("Test");
        entity.Get<Mesh>() = default;

        ref var mesh = ref entity.Get<Mesh>();
        ref var material = ref entity.Get<Material>();
        ref var transform = ref entity.Get<Transform>();

        mesh.Vertices = new float[]
        {
            -0.5f, -0.5f, 0, 0, 0,
            -0.5f, 0.5f, 0, 0, 1,
            0.5f, 0.5f, 0, 1, 1,
            0.5f, -0.5f, 0, 1, 0
        };

        mesh.Indices = new uint[]
        {
            0, 2, 1,
            0, 3, 2
        };
        
        material.AddTexture(
            TextureSource.FromFile("Resources/Textures/cat.png", TextureType.Albedo));

        material.AddShader(
            ShaderSource.FromFile("Resources/Shaders/test.vert"), 
            ShaderSource.FromFile("Resources/Shaders/test.frag"));
    }
}