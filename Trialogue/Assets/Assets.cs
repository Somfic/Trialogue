using System.IO;

namespace Trialogue.Assets
{
    public static class Assets
    {
        public static string RootPath = @"C:\Users\Lucas\Documents\GitHub\Trialogue\Hireath\bin\Debug\net5.0\Assets";

        public static string Get(string path)
        {
            return Path.Combine(RootPath, path);
        }
    }
}