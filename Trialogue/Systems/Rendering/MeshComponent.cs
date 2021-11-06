using System.IO;
using Microsoft.Extensions.Configuration.UserSecrets;
using Trialogue.OpenGl;

namespace Trialogue.Systems.Rendering
{
    public struct MeshComponent
    {
        public float[] Vertices;
        
        internal uint Vao;
        
        internal uint Vbo;
    }

    public struct MaterialComponent
    {
        public void SetShaders(params Shader[] shaders) => Shaders = shaders;
        
        internal Shader[] Shaders;
        internal uint ShaderProgram;
    }
    
    public struct Shader
    {
        internal uint Id;

        internal string Glsl;

        internal ShaderType Type;

        public static Shader FromText(string glsl, ShaderType type)
        {
            return new Shader()
            {
                Type = type,
                Glsl = glsl
            };
        }

        public static Shader FromFile(string path)
        {
            // Check if the shader file exists
            if (!File.Exists(path))
            {
                throw new FileNotFoundException("Could not find shader file", path);
            }

            // Get the shader type based on the file extension
            var info = new FileInfo(path);
            var type = info.Extension.ToLower() switch
            {
                ".frag" => ShaderType.FragmentShader,
                ".vert" => ShaderType.VertexShader,
                ".tesc" => ShaderType.TessControlShader,
                ".tese" => ShaderType.TessEvaluationShader,
                ".geom" => ShaderType.GeometryShader,
                ".comp" => ShaderType.ComputeShader,
                _ => ShaderType.FragmentShader
            };

            var glsl = File.ReadAllText(path);
            
            return new Shader()
            {
                Type = type,
                Glsl = glsl
            };
        }
        
        public enum ShaderType
        {
            FragmentShader = 35632,
            VertexShader = 35633,
            GeometryShader = 36313,
            TessEvaluationShader = 36487,
            TessControlShader = 36488,
            ComputeShader = 37305
        }
    }
}