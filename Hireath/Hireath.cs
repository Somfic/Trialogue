using System;
using System.Drawing;
using System.Numerics;
using Microsoft.Extensions.Logging;
using Trialogue;
using Trialogue.Components;
using Trialogue.Ecs;
using Trialogue.Systems.Rendering;
using Trialogue.Systems.Rendering.Ui;
using Trialogue.Window;

namespace Hireath
{
    internal class Hireath : Window
    {
        private readonly ILogger<Hireath> _log;

        private EcsEntity _camera;
        private EcsEntity _cube;

        public Hireath(ILogger<Hireath> log)
        {
            _log = log;
        }

        public override void OnInitialise()
        {
            AddSystem<RenderSystem>();
            AddSystem<UiRenderSystem>();
            //AddSystem<CameraSystem>();

            _camera = CreateEntity("Camera");
            ref var cameraCamera = ref _camera.Get<Camera>();
            ref var cameraTransform = ref _camera.Get<Transform>();

            cameraCamera.IsOrthographic = false;
            cameraCamera.NearPlane = 0.001f;
            cameraCamera.FarPlane = 10000f;
            cameraCamera.FieldOfView = MathF.PI / 2f;
            cameraCamera.IsFollowingTarget = true;

            cameraTransform.Position = new Vector3(0, 0, 10);

            _cube = CreateEntity("Square");
            ref var model = ref _cube.Get<Model>();
            ref var material = ref _cube.Get<Material>();
            ref var renderer = ref _cube.Get<Renderer>();
            ref var transform = ref _cube.Get<Transform>();

            model.SetModel("Models/sphere.obj", Trialogue.Importer.Shading.Smooth);

            transform.Scale = Vector3.One;

            material.SetShaders(Shader.FromFile("Shaders/vertex.vert"), Shader.FromFile("Shaders/fragment.frag"));
            material.AmbientOcclusion = 1;
            material.Albedo = new Vector3(1, 0, 0);
        }
    }

    internal static class Program
    {
        public static void Main(string[] args)
        {
            var game = TrialogueEngine.Create<Hireath>(new WindowOptions
            {
                TopMost = true,
                StartCentered = false,
                FocusOnShow = false,
                Title = "Hireath",
                Position = new Vector2(0, 40),
                Size = new Size(1000, 800),
                SampleSize = 4,
                Resizable = true,
                VSync = true
            });
            game.Run();
        }
    }
}