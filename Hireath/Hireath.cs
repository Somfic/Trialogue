using System.Numerics;
using Microsoft.Extensions.Logging;
using Trialogue;
using Trialogue.Ecs;
using Trialogue.Systems.Rendering;
using Trialogue.Window;

namespace Hireath
{
    internal class Hireath : Window
    {
        private readonly ILogger<Hireath> _log;

        public Hireath(ILogger<Hireath> log)
        {
            _log = log;
        }

        public override void OnInitialise()
        {
            _log.LogInformation("Hello!");

            var triangle = CreateEntity();
            ref var mesh = ref triangle.Get<MeshComponent>();
            ref var material = ref triangle.Get<MaterialComponent>();
            
            mesh.Vertices = new[] {
                -1f, 1f, 1f, 0f, 0f, // top left
                1f, 1f, 0f, 1f, 0f,// top right
                -1f, -1f, 0f, 0f, 1f, // bottom left

                1f, 1f, 0f, 1f, 0f,// top right
                1f, -1f, 0f, 1f, 1f, // bottom right
                -1f, -1f, 0f, 0f, 1f, // bottom left
            };
            
            string vertexShader = @"#version 330 core
                                    layout (location = 0) in vec2 aPosition;
                                    layout (location = 1) in vec3 aColor;
                                    out vec4 vertexColor;
    
                                    void main() 
                                    {
                                        vertexColor = vec4(aColor.rgb, 1.0);
                                        gl_Position = vec4(aPosition.xy, 0, 1.0);
                                    }";

            string fragmentShader = @"#version 330 core
                                    out vec4 FragColor;
                                    in vec4 vertexColor;

                                    void main() 
                                    {
                                        FragColor = vertexColor;
                                    }";
            
            material.SetShaders(Shader.FromText(vertexShader, Shader.ShaderType.VertexShader), Shader.FromText(fragmentShader, Shader.ShaderType.FragmentShader));
        }
    }

    internal static class Program
    {
        public static void Main(string[] args)
        {
            var game = TrialogueEngine.Create<Hireath>(new WindowOptions()
            {
                TopMost = true,
                StartCentered = false,
                FocusOnShow = false,
                Title = "Hireath",
                Position = new Vector2(2450, 100)
            });
            game.Run();
        }
    }
}