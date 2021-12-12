using System;
using System.Drawing;
using System.IO;
using System.Numerics;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Trialogue;
using Trialogue.Components;
using Trialogue.Ecs;
using Trialogue.Importer;
using Trialogue.Systems.Rendering;
using Trialogue.Window;
using Veldrid;

namespace Hireath
{
    internal class Hireath : Window
    {
        private readonly ILogger<Hireath> _log;
        
        private EcsEntity camera;
        private EcsEntity cube;

        public Hireath(ILogger<Hireath> log)
        {
            _log = log;
        }

        public override void OnInitialise()
        {
            AddSystem<RenderSystem>();
            AddSystem<UiRenderSystem>();
            //AddSystem<CameraSystem>();

            camera = CreateEntity("Camera");
            ref var cameraCamera = ref camera.Get<Camera>();
            ref var cameraTransform = ref camera.Get<Transform>();
    
            cameraCamera.IsOrthographic = false;
            cameraCamera.NearPlane = 0.001f;
            cameraCamera.FarPlane = 10000f;
            cameraCamera.FieldOfView = MathF.PI / 2f;
            cameraCamera.IsFollowingTarget = true;

            cameraTransform.Position = new Vector3(0, 0, 10);

            cube = CreateEntity("Square");
            ref var model = ref cube.Get<Model>();
            ref var material = ref cube.Get<Material>();
            ref var renderer = ref cube.Get<Renderer>();
            ref var transform = ref cube.Get<Transform>();

            model.SetModel("Models/test.3ds");
            
            transform.Scale = Vector3.One;

            material.SetShaders(
                Material.Shader.FromFile("Shaders/vertex.vert"),
                Material.Shader.FromFile("Shaders/fragment.frag"));
        }

        public override void OnUpdate(ref Context context)
        {
            cube.Get<Transform>().Rotation = new Vector3((context.Time.Total * MathF.PI * 10) % 360f);
        }
    }

    internal static class Program
    {
        public static void Main(string[] args)
        {
            var game = TrialogueEngine.Create<Hireath>(new WindowOptions()
            {
                TopMost = true,
                StartCentered = false,
                FocusOnShow = false,
                Title = "Hireath",
                Position = new Vector2(0, 40),
                Size = new Size(1000, 800),
                SampleSize = 4,
                Resizable = true,
                VSync = true,
            });
            game.Run();
        }
    }
}