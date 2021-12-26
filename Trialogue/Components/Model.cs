using System.Collections.Generic;
using System.IO;
using System.Linq;
using ImGuiNET;
using Trialogue.Ecs;
using Trialogue.Importer;
using Veldrid;
using Veldrid.ImageSharp;

namespace Trialogue.Components
{
    public struct Model : IEcsComponent
    {
        internal ProcessedModel ProcessedModel;

        public FileInfo Texture;

        internal IList<ModelResources> Resources;

        internal Texture TextureResource;

        public void SetModel(string path, Shading shadingMode = Shading.Flat)
        {
            var fullPath = Assets.Assets.Get(path);

            using var file = File.OpenRead(fullPath);
            ProcessedModel = new AssimpProcessor().ProcessT(file, Path.GetExtension(fullPath), shadingMode);
        }
        
        public void Dispose()
        {
        }
    }
}