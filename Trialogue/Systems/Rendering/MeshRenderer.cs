using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http.Json;
using System.Numerics;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Trialogue.Components;
using Trialogue.Ecs;
using Trialogue.Glfw;
using Trialogue.OpenGl;
using Trialogue.Structures.Textures;
using Trialogue.Windows;
using Exception = System.Exception;

namespace Trialogue.Systems.Rendering;

public class MeshRenderer : IEcsUpdateSystem, IEcsInitialiseSystem
{
    private readonly ILogger<MeshRenderer>? _log;
    private readonly WindowManager _window;

    private EcsFilter<Mesh, Material, Transform> _meshes;

    public MeshRenderer(ILogger<MeshRenderer>? log, WindowManager window)
    {
        _log = log;
        _window = window;
    }

    public void OnUpdate(ref Context context)
    {
        foreach (var i in _meshes)
        {
            ref var mesh = ref _meshes.Get1(i);
            ref var material = ref _meshes.Get2(i);
            ref var transform = ref _meshes.Get3(i);

            if (!mesh.IsCompiled)
            {
                CompileMesh(ref context, ref mesh);
                mesh.IsCompiled = true;
            }

            if (!material.IsCompiled)
            {
                CompileShaders(ref context, ref material);
                CompileTextures(ref context, ref material);
                material.IsCompiled = true;
            }

            DrawMesh(ref context, ref mesh, ref material, ref transform);
        }
    }

    private unsafe void DrawMesh(ref Context context, ref Mesh mesh, ref Material material, ref Transform transform)
    {
        GL.UseProgram(material.ShaderProgram);

        for (var index = 0; index < material.Textures.Length; index++)
        {
            ref var texture = ref material.Textures[index];

            var uniform = GL.GetUniformLocation(material.ShaderProgram, $"{texture.Type}Texture");
            GL.Uniform1i(uniform, 0);
            GL.BindTexture((int) texture.Dimensions, texture.Id);
        }

        var model = Matrix4x4.CreateScale(transform.Scale);

        var view = Matrix4x4.CreateTranslation(new Vector3(0.0f, -0.5f, -2.0f));
        
        var projection = Matrix4x4.CreatePerspectiveFieldOfView(NumericExtensions.ToRadians(45), (float) _window.Size.Width / _window.Size.Height, 0.1f, 100.0f);

        var modelLocation = GL.GetUniformLocation(material.ShaderProgram, "model");
        var viewLocation = GL.GetUniformLocation(material.ShaderProgram, "view");
        var projectionLocation = GL.GetUniformLocation(material.ShaderProgram, "projection");
        
        GL.UniformMatrix4fv(modelLocation, 1, false, model.ToArray());
        GL.UniformMatrix4fv(viewLocation, 1, false, view.ToArray());
        GL.UniformMatrix4fv(projectionLocation, 1, false, projection.ToArray());
        
        GL.BindVertexArray(mesh.VAO);
        GL.DrawElements(GL.TRIANGLES, mesh.Vertices.Length, GL.UNSIGNED_INT, GL.NULL);
    }

    private unsafe void CompileMesh(ref Context context, ref Mesh mesh)
    {
        mesh.VAO = GL.GenVertexArray();
        mesh.VBO = GL.GenBuffer();
        mesh.EBO = GL.GenBuffer();

        GL.BindVertexArray(mesh.VAO);

        GL.BindBuffer(GL.ARRAY_BUFFER, mesh.VBO);
        fixed (float* vertices = &mesh.Vertices[0])
            GL.BufferData(GL.ARRAY_BUFFER, mesh.Vertices.Length * sizeof(float), vertices, GL.STATIC_DRAW);

        GL.BindBuffer(GL.ELEMENT_ARRAY_BUFFER, mesh.EBO);
        fixed (uint* indices = &mesh.Indices[0])
            GL.BufferData(GL.ELEMENT_ARRAY_BUFFER, sizeof(uint) * mesh.Indices.Length, indices, GL.STATIC_DRAW);

        GL.VertexAttribPointer(0, 3, GL.FLOAT, false, 5 * sizeof(float), (void*) 0);
        GL.EnableVertexAttribArray(0);

        GL.VertexAttribPointer(1, 2, GL.FLOAT, false, 5 * sizeof(float), (void*) (3 * sizeof(float)));
        GL.EnableVertexAttribArray(1);

        //GL.VertexAttribPointer(1, 3, GL.FLOAT, false, 5 * sizeof(float), (void*)(2 * sizeof(float)));

        GL.BindBuffer(GL.ARRAY_BUFFER, 0);
        GL.BindVertexArray(0);
        GL.BindBuffer(GL.ELEMENT_ARRAY_BUFFER, 0);
    }

    private unsafe void CompileTextures(ref Context context, ref Material material)
    {
        for (var index = 0; index < material.Textures.Length; index++)
        {
            ref var texture = ref material.Textures[index];

            try
            {
                var image = Image.Load<Rgba32>(texture.Source);
                image.Mutate(x => x.Flip(FlipMode.Vertical));
                var pixels = new List<byte>(4 * image.Width * image.Height);
                for (var y = 0; y < image.Height; y++)
                {
                    var row = image.GetPixelRowSpan(y);

                    for (var x = 0; x < image.Width; x++)
                    {
                        pixels.Add(row[x].R);
                        pixels.Add(row[x].G);
                        pixels.Add(row[x].B);
                        pixels.Add(row[x].A);
                    }
                }

                texture.Id = GL.GenTexture();

                var textureTarget = texture.Type switch
                {
                    TextureType.Albedo => GL.TEXTURE0
                };

                GL.ActiveTexture(textureTarget);
                GL.BindTexture((int) texture.Dimensions, texture.Id);

                GL.TexParameteri((int) texture.Dimensions, GL.TEXTURE_MIN_FILTER, GL.NEAREST); //todo: add configurable filter
                GL.TexParameteri((int) texture.Dimensions, GL.TEXTURE_MAG_FILTER, GL.NEAREST);

                GL.TexParameteri((int) texture.Dimensions, GL.TEXTURE_WRAP_S, GL.REPEAT);
                GL.TexParameteri((int) texture.Dimensions, GL.TEXTURE_WRAP_T, GL.REPEAT);

                fixed (byte* i = &pixels.ToArray()[0])
                    GL.TexImage2D((int) texture.Dimensions, 0, GL.RGBA8, image.Width, image.Height, 0, GL.RGBA, GL.UNSIGNED_BYTE, i);

                GL.GenerateMipmap((int) texture.Dimensions);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Could not compile {TextureType} texture", texture.Type.ToString());
            }
        }

        _log.LogDebug("Compiled {TextureCount} textures", material.Textures.Length);
    }

    private void CompileShaders(ref Context context, ref Material material)
    {
        material.ShaderProgram = GL.CreateProgram();

        for (var index = 0; index < material.Shaders.Length; index++)
        {
            ref var shader = ref material.Shaders[index];
            shader.Id = GL.CreateShader((int) shader.Type);

            GL.ShaderSource(shader.Id, shader.Source);
            GL.CompileShader(shader.Id);

            var shaderStatus = GL.GetShaderiv(shader.Id, GL.COMPILE_STATUS, 1);

            if (shaderStatus[0] == 0)
            {
                var error = GL.GetShaderInfoLog(shader.Id);
                var ex = new Exception($"Could not compile shader: {error}");
                _log.LogError(ex, "Could not compile {ShaderType} shader", shader.Type.ToString().ToLower());
            }
            else
            {
                GL.AttachShader(material.ShaderProgram, shader.Id);
            }
        }

        GL.LinkProgram(material.ShaderProgram);
        var programStatus = GL.GetProgramiv(material.ShaderProgram, GL.LINK_STATUS, 1);

        if (programStatus[0] == 0)
        {
            var error = GL.GetProgramInfoLog(material.ShaderProgram);
            var ex = new Exception($"Could not link shaders: {error}");
            _log.LogError(ex, "Could not link shaders");
        }

        for (var index = 0; index < material.Shaders.Length; index++)
        {
            ref var shader = ref material.Shaders[index];

            GL.DetachShader(material.ShaderProgram, shader.Id);
            GL.DeleteShader(shader.Id);
        }
    }

    public void OnInitialise()
    {
        foreach (var i in _meshes)
        {
            ref var mesh = ref _meshes.Get1(i);
            ref var material = ref _meshes.Get2(i);
            ref var transform = ref _meshes.Get3(i);

            _log.LogInformation("Start values of Transform: {Transform}", JsonConvert.SerializeObject(transform));
        }
    }
}