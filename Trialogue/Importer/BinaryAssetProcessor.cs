using System.IO;

namespace Trialogue.Importer;

public abstract class BinaryAssetProcessor
{
    public abstract object Process(Stream stream, string extension, object options);
}

public abstract class BinaryAssetProcessor<T> : BinaryAssetProcessor
{
    public override object Process(Stream stream, string extension, object shading)
    {
        return ProcessT(stream, extension, (Shading) shading);
    }

    public abstract T ProcessT(Stream stream, string extension, Shading shading);
}