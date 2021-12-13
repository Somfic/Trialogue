using System.IO;
using Veldrid;

namespace Trialogue.Components
{
    public struct Shader
    {
        public string Source;

        public ShaderStages Stage;

        public static Shader FromText(string source, ShaderStages stage)
        {
            return new Shader
            {
                Source = source,
                Stage = stage
            };
        }

        public static Shader FromFile(string path, ShaderStages stage)
        {
            return new Shader
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

            return new Shader
            {
                Source = File.ReadAllText(fullPath),
                Stage = stage
            };
        }
    }
}