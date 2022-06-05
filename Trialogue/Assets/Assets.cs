using System.IO;

namespace Trialogue.Assets;

public static class Assets
{
    public static string RootPath = @"./Assets/";

    public static string Get(string path)
    {
        return Path.Combine(RootPath, path);
    }
}