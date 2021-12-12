using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using ImGuiNET;
using Trialogue.Ecs;
using Veldrid;

namespace Trialogue.Components
{
    public struct Material : IEcsComponent
    {
        public Veldrid.Shader[] Shaders;

        internal ShaderDescription[] ShaderDescriptions;
        public void SetShaders(params Shader[] shaders) => ShaderDescriptions = shaders.Select(shader => new ShaderDescription(shader.Stage, Encoding.UTF8.GetBytes((string) shader.Source), "main")).ToArray();

        public struct Shader
        {
            public string Source;
            
            public ShaderStages Stage;

            public static Shader FromText(string source, ShaderStages stage)
            {
                return new Shader()
                {
                    Source = source,
                    Stage = stage
                };
            }
            
            public static Shader FromFile(string path, ShaderStages stage)
            {
                return new Shader()
                {
                    Source = File.ReadAllText(path),
                    Stage = stage
                };
            }
            
            public static Shader FromFile(string path)
            {
                var fullPath = Assets.Assets.Get(path);
                
                var stage = Path.GetExtension(fullPath) switch
                {
                    ".frag" => ShaderStages.Fragment,
                    ".vert" => ShaderStages.Vertex,
                    ".geom" => ShaderStages.Geometry,
                    ".tesc" => ShaderStages.TessellationControl,
                    ".tese" => ShaderStages.TessellationEvaluation,
                    ".comp" => ShaderStages.Compute,
                    _ => ShaderStages.Fragment
                };

                return new Shader()
                {
                    Source = File.ReadAllText(fullPath),
                    Stage = stage
                };
            }
        }

        public void DrawUi(ref EcsEntity ecsEntity)
        {
            ecsEntity.Update(this);
        }

        public void Dispose()
        {
            ShaderDescriptions = Array.Empty<ShaderDescription>();
            Shaders = Array.Empty<Veldrid.Shader>();
        }
    }
}