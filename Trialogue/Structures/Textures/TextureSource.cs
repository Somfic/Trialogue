using System.IO;

namespace Trialogue.Structures.Textures;

public struct TextureSource
{
    public uint Id { get; internal set; } = 123;
    public TextureDimensions Dimensions { get; init; }
    public TextureType Type { get; init; }
    internal Stream Source { get; init; }

    public static TextureSource FromFile(string path, TextureType type, TextureDimensions dimensions = TextureDimensions.Two)
    {
        var file = new FileInfo(path);

        if (!file.Exists)
            throw new FileNotFoundException("Could not find shader file", path);
        
        return new TextureSource()
        {
            Source = File.OpenRead(file.FullName),
            Dimensions = dimensions,
            Type = type
        };
    }
}