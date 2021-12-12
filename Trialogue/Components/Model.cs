using System.Collections.Generic;
using System.IO;
using System.Linq;
using ImGuiNET;
using Trialogue.Ecs;
using Trialogue.Importer;

namespace Trialogue.Components
{
    public struct Model : IEcsComponent
    {
        internal ProcessedModel ProcessedModel;
        internal IList<ModelResources> Resources;

        private long _verticesCount;

        public void SetModel(string path, Shading shadingMode = Shading.Flat)
        {
            var fullPath = Assets.Assets.Get(path);

            using var file = File.OpenRead(fullPath);
            ProcessedModel = new AssimpProcessor().ProcessT(file, Path.GetExtension(fullPath), shadingMode);

            _verticesCount = ProcessedModel.MeshParts.Sum(x => x.VertexElements.Length);
        }

        public void DrawUi(ref EcsEntity ecsEntity)
        {
            if (ProcessedModel == null)
            {
                ImGui.Text("No model loaded");
                return;
            }

            foreach (var mesh in ProcessedModel.MeshParts) ImGui.Text($"{mesh.Name} ({mesh.IndexCount} vertices)");
        }
        
        public void Dispose()
        {
        }
    }
}