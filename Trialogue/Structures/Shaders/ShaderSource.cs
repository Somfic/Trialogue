using System.IO;

namespace Trialogue.Structures.Shaders;

public struct ShaderSource
{
    internal string Source;
    public uint Id { get; internal set; }
    
    public ShaderType Type { get; set; }

    public static ShaderSource FromFile(string path)
    {
        var file = new FileInfo(path);

        if (!file.Exists)
            throw new FileNotFoundException("Could not find shader file", path);

        var type = file.Extension.ToLower() switch
        {
            ".frag" => ShaderType.Fragment,
            ".vert" => ShaderType.Vertex,
            ".tesc" => ShaderType.TessControl,
            ".tese" => ShaderType.TessEvaluation,
            ".geom" => ShaderType.Geometry,
            ".comp" => ShaderType.Compute,
            _ => ShaderType.Fragment
        };

        return FromFile(path, type);
    }
    
    public static ShaderSource FromFile(string path, ShaderType type)
    {
        var file = new FileInfo(path);

        if (!file.Exists)
            throw new FileNotFoundException("Could not find shader file", path);
        
        return new ShaderSource()
        {
            Source = File.ReadAllText(file.FullName),
            Type = type
        };
    }
}