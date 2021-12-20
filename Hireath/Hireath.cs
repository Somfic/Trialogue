using System;
using System.Drawing;
using System.Numerics;
using BepuPhysics.Collidables;
using Microsoft.Extensions.Logging;
using Trialogue;
using Trialogue.Components;
using Trialogue.Ecs;
using Trialogue.Systems;
using Trialogue.Systems.Physics;
using Trialogue.Systems.Rendering;
using Trialogue.Systems.Rendering.Ui;
using Trialogue.Window;

namespace Hireath
{
    internal class Hireath : Window
    {
        private readonly ILogger<Hireath> _log;

        private EcsEntity _camera;
        private EcsEntity _cow;
        private EcsEntity _floor;

        public Hireath(ILogger<Hireath> log)
        {
            _log = log;
        }

        public override void OnInitialise()
        {
            AddSystem<RenderSystem>();
            AddSystem<UiRenderSystem>();
            AddSystem<PhysicsSystem>();

            _camera = CreateEntity("Camera");
            ref var cameraCamera = ref _camera.Get<Camera>();
            ref var cameraTransform = ref _camera.Get<Transform>();

            cameraCamera.IsOrthographic = false;
            cameraCamera.NearPlane = 0.001f;
            cameraCamera.FarPlane = 10000f;
            cameraCamera.FieldOfView = MathF.PI / 2f;
            cameraCamera.IsFollowingTarget = true;

            cameraTransform.Position = new Vector3(0, 0, 10);

            CreateCow();
            CreateFloor();
        }

        public override void OnUpdate(ref Context context)
        {
            ref var camera = ref _camera.Get<Camera>();
            
            // Follow the cow
            camera.Target = _cow.Get<Transform>().Position;
        }

        void CreateCow()
        {
            _cow = CreateEntity("Cow");
            ref var model = ref _cow.Get<Model>();
            ref var material = ref _cow.Get<Material>();
            ref var renderer = ref _cow.Get<Renderer>();
            ref var transform = ref _cow.Get<Transform>();
            ref var body = ref _cow.Get<Body>();
            body.Shape = new Sphere(1);

            model.SetModel("Models/cow.obj", Trialogue.Importer.Shading.Smooth);
            transform.Scale = Vector3.One;
            transform.Position = new Vector3(0, 10, 0);
            material.SetShaders(Shader.FromFile("Shaders/vertex.vert"), Shader.FromFile("Shaders/fragment.frag"));
            material.AmbientOcclusion = 1;
            material.Albedo = new Vector3(1, 0, 0);
        }

        void CreateFloor()
        {
            _floor = CreateEntity("Floor");
            ref var model = ref _floor.Get<Model>();
            ref var material = ref _floor.Get<Material>();
            //ref var renderer = ref _floor.Get<Renderer>();
            ref var transform = ref _floor.Get<Transform>();
            ref var body = ref _floor.Get<Static>();
            body.Shape = new Box(10, 0.1f, 10);
            
            //model.SetModel("Models/floor.obj", Trialogue.Importer.Shading.Smooth);
            transform.Scale = Vector3.One;
            transform.Rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, MathF.PI / 5f);
            material.SetShaders(Shader.FromFile("Shaders/vertex.vert"), Shader.FromFile("Shaders/fragment.frag"));
            material.AmbientOcclusion = 1;
            material.Albedo = new Vector3(1, 1, 1);
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