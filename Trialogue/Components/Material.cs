using System;
using System.Drawing;
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
        internal Veldrid.Shader[] Shaders;

        internal ShaderDescription[] ShaderDescriptions;
        
        internal ResourceSet MaterialSet;

        public Vector3 Albedo;

        public float Metallic;

        public float Roughness;
        public float AmbientOcclusion;
        
        internal DeviceBuffer AlbedoBuffer;
        internal DeviceBuffer MetallicBuffer;
        internal DeviceBuffer RoughnessBuffer;
        internal DeviceBuffer AmbientOcclusionBuffer;

        public void SetShaders(params Shader[] shaders)
        {
            ShaderDescriptions = shaders.Select(shader =>
                new ShaderDescription(shader.Stage, Encoding.UTF8.GetBytes(shader.Source), "main")).ToArray();
        }

        public void DrawUi(ref EcsEntity ecsEntity)
        {
            ImGui.ColorEdit3("Albedo", ref Albedo);
            ImGui.SliderFloat("Metallic", ref Metallic, 0, 1);
            ImGui.SliderFloat("Roughness", ref Roughness, 0, 1);
            ImGui.SliderFloat("Ambient occlusion", ref AmbientOcclusion, 0, 1);
            
            ecsEntity.Update(this);
        }

        public void Dispose()
        {
            ShaderDescriptions = Array.Empty<ShaderDescription>();
            Shaders = Array.Empty<Veldrid.Shader>();

            AlbedoBuffer.Dispose();
            RoughnessBuffer.Dispose();
            MetallicBuffer.Dispose();
        }
    }
}