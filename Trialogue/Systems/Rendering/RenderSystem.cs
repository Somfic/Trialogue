using System;
using System.Linq;
using System.Numerics;
using Microsoft.Extensions.Logging;
using Trialogue.Components;
using Trialogue.Ecs;
using Trialogue.Window;
using Veldrid;
using Veldrid.SPIRV;
using Shader = Veldrid.Shader;

namespace Trialogue.Systems.Rendering
{
    public class RenderSystem : IEcsStartSystem, IEcsRenderSystem, IEcsDestroySystem
    {
        private readonly ILogger<RenderSystem> _log;
        private EcsFilter<Camera, Transform> _cameraFilter;

        private EcsFilter<Model, Material, Transform, Renderer> _entities;
        private EcsFilter<Light, Transform> _lights;
        
        private VertexLayoutDescription _sharedVertexLayout;

        private ResourceLayout _cameraLayout;
        private ResourceLayout _worldLayout;
        private ResourceLayout _materialLayout;
        private ResourceLayout _lightLayout;

        public RenderSystem(ILogger<RenderSystem> log)
        {
            _log = log;
        }

        public void OnStart(ref Context context)
        {
            var graphicsDevice = context.Window.GraphicsDevice;
            var resourceFactory = graphicsDevice.ResourceFactory;

            _sharedVertexLayout = new VertexLayoutDescription(
                new VertexElementDescription("Position", VertexElementSemantic.TextureCoordinate,
                    VertexElementFormat.Float3),
                new VertexElementDescription("Normal", VertexElementSemantic.TextureCoordinate,
                    VertexElementFormat.Float3),
                new VertexElementDescription("TexCoord", VertexElementSemantic.TextureCoordinate,
                    VertexElementFormat.Float2));

            // Set 0 - Camera
            ref var camera = ref _cameraFilter.GetEntity(0);
            ref var cameraCamera = ref camera.Get<Camera>();
            ref var cameraTransform = ref camera.Get<Transform>();
            
            cameraCamera.ProjectionUniform = new Uniform<Matrix4x4>("Projection");
            cameraCamera.ViewUniform = new Uniform<Matrix4x4>("View");
            cameraTransform.PositionUniform = new Uniform<Vector3>("Position");

            cameraCamera.UniformSet = new UniformSet(0, cameraCamera.ProjectionUniform, cameraCamera.ViewUniform, cameraTransform.PositionUniform);
            _cameraLayout = cameraCamera.UniformSet.CreateLayout(resourceFactory);

            // Set 3 - Lights
            foreach (var i in _lights)
            {
                ref var light = ref _lights.Get1(i);
                ref var transform = ref _lights.Get2(i);

                light.ColorUniform = new Uniform<Vector3>("Color");
                light.StrengthUniform = new Uniform<float>("Strength");
                transform.PositionUniform = new Uniform<Vector3>("Position");
                
                light.UniformSet = new UniformSet(3, light.ColorUniform, light.StrengthUniform, transform.PositionUniform);
                _lightLayout ??= light.UniformSet.CreateLayout(resourceFactory);
            }
            
            foreach (var i in _entities)
            {
                ref var model = ref _entities.Get1(i);
                ref var material = ref _entities.Get2(i);
                ref var transform = ref _entities.Get3(i);
                ref var renderer = ref _entities.Get4(i);

                // Set 1 - World
                transform.ModelUniform = new Uniform<Matrix4x4>("Model");
                transform.UniformSet = new UniformSet(1, transform.ModelUniform);
                _worldLayout = transform.UniformSet.CreateLayout(resourceFactory);
                
                // Set 2 - Materials
                material.AlbedoUniform = new Uniform<Vector3>("Albedo");
                material.MetallicUniform = new Uniform<float>("Metallic");
                material.RoughnessUniform = new Uniform<float>("Roughness");
                material.AmbientOcclusionUniform = new Uniform<float>("AmbientOcclusion");
                
                material.UniformSet = new UniformSet(2, material.AlbedoUniform, material.MetallicUniform, material.RoughnessUniform, material.AmbientOcclusionUniform);
                _materialLayout ??= material.UniformSet.CreateLayout(resourceFactory);
                
                // Build model resources
                model.Resources ??= model.ProcessedModel.MeshParts.Select(x => x.CreateDeviceResources(graphicsDevice, resourceFactory)).ToList();
                
                // Setup the pipeline
                renderer.PipeLine ??= CreatePipeline(graphicsDevice, resourceFactory, material.Shaders);
            }
        }

        private Pipeline CreatePipeline(GraphicsDevice graphicsDevice, ResourceFactory resourceFactory, Shader[] shaders)
        {
            var pipelineDescription = new GraphicsPipelineDescription
            {
                BlendState = BlendStateDescription.SingleOverrideBlend,
                PrimitiveTopology = PrimitiveTopology.TriangleList,
                ResourceLayouts = new[] {_cameraLayout, _worldLayout, _materialLayout, _lightLayout},

                DepthStencilState = new DepthStencilStateDescription
                {
                    DepthTestEnabled = true,
                    DepthWriteEnabled = true,
                    DepthComparison = ComparisonKind.LessEqual,
                    StencilTestEnabled = true
                },

                RasterizerState = new RasterizerStateDescription
                {
                    CullMode = FaceCullMode.Front,
                    FillMode = PolygonFillMode.Solid,
                    FrontFace = FrontFace.CounterClockwise,
                    DepthClipEnabled = true,
                    ScissorTestEnabled = true
                },
                ShaderSet = new ShaderSetDescription(new[] {_sharedVertexLayout}, shaders),
                Outputs = graphicsDevice.SwapchainFramebuffer.OutputDescription
            };

            return resourceFactory.CreateGraphicsPipeline(pipelineDescription);
        }

        public void OnRender(ref Context context)
        {
            var graphicsDevice = context.Window.GraphicsDevice;
            var resourceFactory = graphicsDevice.ResourceFactory;
            var commandList = context.Window.CommandList;

            if (_cameraFilter.IsEmpty())
            {
                commandList.ClearColorTarget(0, RgbaFloat.Blue);
                return;
            }

            ref var cameraEntity = ref _cameraFilter.GetEntity(0);
            ref var camera = ref cameraEntity.Get<Camera>();
            ref var cameraTransform = ref cameraEntity.Get<Transform>();

            var projectionMatrix = camera.CalculateProjectionMatrix(ref context);
            var viewMatrix = camera.CalculateViewMatrix(ref cameraTransform);

            int amountOfLights = _lights.GetEntitiesCount();

            Vector3[] lightPositions = new Vector3[128];
            Vector3[] lightColors = new Vector3[128];
            float[] lightStrengths = new float[128];

            foreach (var i in _lights)
            {
                ref var light = ref _lights.Get1(i);
                ref var lightTransform = ref _lights.Get2(i);

                lightPositions[i] = lightTransform.Position;
                lightColors[i] = light.Color;
                lightStrengths[i] = light.Strength;
            }

            foreach (var i in _entities)
            {
                ref var model = ref _entities.Get1(i);
                ref var material = ref _entities.Get2(i);
                ref var transform = ref _entities.Get3(i);
                ref var renderer = ref _entities.Get4(i);

                var modelMatrix = transform.CalculateModelMatrix(ref context);

                commandList.ClearColorTarget(0, RgbaFloat.Black);
                
                camera.ProjectionUniform.Update(commandList, ref projectionMatrix);
                camera.ViewUniform.Update(commandList, ref viewMatrix);
                cameraTransform.PositionUniform.Update(commandList, ref cameraTransform.Position);
                
                transform.PositionUniform.Update(commandList, ref transform.Position);
                transform.RotationUniform.Update(commandList, ref transform.Rotation);
                transform.ScaleUniform.Update(commandList, ref transform.Scale);
                
                material.AmbientOcclusionUniform.Update(commandList, ref material.AmbientOcclusion);
                material.AlbedoUniform.Update(commandList, ref material.Albedo);
                material.MetallicUniform.Update(commandList, ref material.Metallic);
                material.RoughnessUniform.Update(commandList, ref material.Roughness);
   
                // commandList.UpdateBuffer(transform.ModelBuffer, 0, ref modelMatrix);
                worldSetModel.Update(commandList, ref modelMatrix);

                commandList.UpdateBuffer(material.AlbedoBuffer, 0, ref material.Albedo);
                commandList.UpdateBuffer(material.MetallicBuffer, 0, ref material.Metallic);
                commandList.UpdateBuffer(material.RoughnessBuffer, 0, ref material.Roughness);
                commandList.UpdateBuffer(material.AmbientOcclusionBuffer, 0, ref material.AmbientOcclusion);

                commandList.UpdateBuffer(_amountOfLightsBuffer, 0, ref amountOfLights);
                commandList.UpdateBuffer(_lightPositionsBuffer, 0, lightPositions);
                commandList.UpdateBuffer(_lightColorsBuffer, 0, lightColors);
                commandList.UpdateBuffer(_lightStrengthsBuffer, 0, lightStrengths);

                commandList.SetPipeline(renderer.PipeLine);

                foreach (var modelResource in model.Resources)
                {
                    commandList.SetVertexBuffer(0, modelResource.VertexBuffer);
                    commandList.SetIndexBuffer(modelResource.IndexBuffer, modelResource.IndexFormat);

                    commandList.SetGraphicsResourceSet(0, camera.UniformSet.CreateSet());
                    commandList.SetGraphicsResourceSet(worldSet.SetNumber, worldSet.Set);
                    commandList.SetGraphicsResourceSet(2, material.MaterialSet);
                    commandList.SetGraphicsResourceSet(3, _lightSet);

                    commandList.DrawIndexed(modelResource.IndexCount, 1, 0, 0, 0);
                }
            }
        }

        public void OnDestroy(ref Context context)
        {
            foreach (var i in _entities)
            {
                ref var mesh = ref _entities.Get1(i);
                ref var material = ref _entities.Get2(i);
                ref var renderer = ref _entities.Get3(i);

                mesh.Dispose();
                material.Dispose();
                renderer.Dispose();
            }
        }
    }
}