using System;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Text;
using ImGuiNET;
using System.ComponentModel.DataAnnotations;
using System.IO;
using Trialogue.Ecs;
using Trialogue.Systems.Rendering.Ui;
using Veldrid;

namespace Trialogue.Components;

public struct Material : IEcsComponent
{
    internal Veldrid.Shader[] Shaders;

    internal ShaderDescription[] ShaderDescriptions;
        
    internal ResourceSet MaterialSet;

    [Color]
    public Vector3 Albedo;

    [Range(0, 1)]
    public float Metallic;

    [Range(0, 1)]
    public float Roughness;

    [Range(0, 1)]
    public float AmbientOcclusion;
        
    public FileInfo Texture;
        
    internal DeviceBuffer AlbedoBuffer;
    internal DeviceBuffer MetallicBuffer;
    internal DeviceBuffer RoughnessBuffer;
    internal DeviceBuffer AmbientOcclusionBuffer;

    public void SetShaders(params Shader[] shaders)
    {
        ShaderDescriptions = shaders.Select(shader =>
            new ShaderDescription(shader.Stage, Encoding.UTF8.GetBytes(shader.Source), "main")).ToArray();
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