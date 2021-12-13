using System;
using System.Linq;
using Microsoft.Extensions.Logging;
using Trialogue.Components;
using Trialogue.Ecs;
using Trialogue.Window;
using Veldrid;
using Veldrid.SPIRV;

namespace Trialogue.Systems.Rendering
{
    public class RenderSystem : IEcsStartSystem, IEcsUpdateSystem, IEcsRenderSystem, IEcsDestroySystem
    {
        private readonly ILogger<RenderSystem> _log;
        private EcsFilter<Camera, Transform> _cameraFilter;

        private EcsFilter<Model, Material, Transform, Renderer> _filter;
        private EcsFilter<Light, Transform> _lights;

        private EcsWorld _world = null;

        private ResourceLayout _cameraSetLayout;
        private ResourceLayout _materialLayout;
        private ResourceLayout _worldLayout;
        private ResourceLayout _lightLayout;

        private VertexLayoutDescription _sharedVertexLayout;

        public RenderSystem(ILogger<RenderSystem> log)
        {
            _log = log;
        }

        public void OnStart(ref Context context)
        {
            var graphicsDevice = context.Window.GraphicsDevice;
            var resourceFactory = graphicsDevice.ResourceFactory;

            var amountOfObjects = _filter.GetEntitiesCount();
            var amountOfLights = _lights.GetEntitiesCount();

            _log.LogInformation($"Found {amountOfObjects} {(amountOfObjects != 1 ? "objects" : "object")} to render.");
            _log.LogInformation($"Found {amountOfLights} {(amountOfLights != 1 ? "lights" : "light")} to render.");

            _sharedVertexLayout = new VertexLayoutDescription(
                new VertexElementDescription("Position", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float3),
                new VertexElementDescription("Normal", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float3),
                new VertexElementDescription("TexCoord", VertexElementSemantic.TextureCoordinate, VertexElementFormat.Float2));


            // Set 0
            _cameraSetLayout = resourceFactory.CreateResourceLayout(
                new ResourceLayoutDescription(
                    new ResourceLayoutElementDescription("ProjectionBuffer", ResourceKind.UniformBuffer, ShaderStages.Vertex),
                    new ResourceLayoutElementDescription("ViewBuffer", ResourceKind.UniformBuffer, ShaderStages.Vertex),
                    new ResourceLayoutElementDescription("PositionBuffer", ResourceKind.UniformBuffer, ShaderStages.Vertex | ShaderStages.Fragment)));

            // Set 1
            _worldLayout = resourceFactory.CreateResourceLayout(
                new ResourceLayoutDescription(new ResourceLayoutElementDescription("ModelBuffer", ResourceKind.UniformBuffer, ShaderStages.Vertex)));

            // Set 2
            _materialLayout = resourceFactory.CreateResourceLayout(
                new ResourceLayoutDescription(
                    new ResourceLayoutElementDescription("AlbedoBuffer", ResourceKind.UniformBuffer, ShaderStages.Fragment),
                    new ResourceLayoutElementDescription("MetallicBuffer", ResourceKind.UniformBuffer, ShaderStages.Fragment),
                    new ResourceLayoutElementDescription("RoughnessBuffer", ResourceKind.UniformBuffer, ShaderStages.Fragment),
                    new ResourceLayoutElementDescription("AmbientOcclusionBuffer", ResourceKind.UniformBuffer, ShaderStages.Fragment)
                ));

            // Set 3
            _lightLayout = resourceFactory.CreateResourceLayout(
                new ResourceLayoutDescription(
                    new ResourceLayoutElementDescription("LightBuffer", ResourceKind.UniformBuffer, ShaderStages.Fragment)));
        }

        public void OnRender(ref Context context)
        {
            CreateResources(ref context);

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

            foreach (var i in _filter)
            {
                ref var model = ref _filter.Get1(i);
                ref var material = ref _filter.Get2(i);
                ref var transform = ref _filter.Get3(i);
                ref var renderer = ref _filter.Get4(i);

                var modelMatrix = transform.CalculateModelMatrix(ref context);

                commandList.ClearColorTarget(0, RgbaFloat.Black);

                commandList.UpdateBuffer(camera.ProjectionBuffer, 0, ref projectionMatrix);
                commandList.UpdateBuffer(camera.ViewBuffer, 0, ref viewMatrix);
                commandList.UpdateBuffer(camera.PositionBuffer, 0, ref cameraTransform.Position);

                commandList.UpdateBuffer(transform.ModelBuffer, 0, ref modelMatrix);
            
                commandList.UpdateBuffer(material.AlbedoBuffer, 0, ref material.Albedo);
                commandList.UpdateBuffer(material.MetallicBuffer, 0, ref material.Metallic);
                commandList.UpdateBuffer(material.RoughnessBuffer, 0, ref material.Roughness);
                commandList.UpdateBuffer(material.AmbientOcclusionBuffer, 0, ref material.AmbientOcclusion);

                commandList.SetPipeline(renderer.PipeLine);

                foreach (var modelResource in model.Resources)
                {
                    commandList.SetVertexBuffer(0, modelResource.VertexBuffer);
                    commandList.SetIndexBuffer(modelResource.IndexBuffer, modelResource.IndexFormat);

                    commandList.SetGraphicsResourceSet(0, camera.ResourceSet);
                    commandList.SetGraphicsResourceSet(1, transform.WorldSet);
                    commandList.SetGraphicsResourceSet(2, material.MaterialSet);

                    commandList.DrawIndexed(modelResource.IndexCount, 1, 0, 0, 0);
                }
            }
        }

        public void OnDestroy(ref Context context)
        {
            foreach (var i in _filter)
            {
                ref var mesh = ref _filter.Get1(i);
                ref var material = ref _filter.Get2(i);
                ref var renderer = ref _filter.Get3(i);

                mesh.Dispose();
                material.Dispose();
                renderer.Dispose();
            }
        }

        private void CreateResources(ref Context context)
        {
            var graphicsDevice = context.Window.GraphicsDevice;
            var resourceFactory = graphicsDevice.ResourceFactory;

            if (_cameraFilter.IsEmpty())
            {
                return;
            }

            ref var cameraEntity = ref _cameraFilter.GetEntity(0);
            ref var camera = ref cameraEntity.Get<Camera>();
            ref var cameraTransform = ref cameraEntity.Get<Camera>();

            // Camera
            camera.ProjectionBuffer ??= resourceFactory.CreateBuffer(new BufferDescription(64, BufferUsage.UniformBuffer));
            camera.ViewBuffer ??= resourceFactory.CreateBuffer(new BufferDescription(64, BufferUsage.UniformBuffer));
            camera.PositionBuffer ??= resourceFactory.CreateBuffer(new BufferDescription(64, BufferUsage.UniformBuffer));

            foreach (var i in _filter)
            {
                ref var model = ref _filter.Get1(i);
                ref var material = ref _filter.Get2(i);
                ref var transform = ref _filter.Get3(i);
                ref var renderer = ref _filter.Get4(i);

                // Transform
                transform.ModelBuffer ??= resourceFactory.CreateBuffer(new BufferDescription(64, BufferUsage.UniformBuffer));

                // Model
                model.Resources ??=
                    model.ProcessedModel.MeshParts
                        .Select(x => x.CreateDeviceResources(graphicsDevice, resourceFactory)).ToList();

                if (material.Shaders == null)
                {
                    // Shaders
                    var vertex = material.ShaderDescriptions.First(x => x.Stage == ShaderStages.Vertex);
                    var fragment = material.ShaderDescriptions.First(x => x.Stage == ShaderStages.Fragment);
                    material.Shaders = resourceFactory.CreateFromSpirv(vertex, fragment);
                }

                material.AlbedoBuffer ??= resourceFactory.CreateBuffer(new BufferDescription(64, BufferUsage.UniformBuffer));
                material.MetallicBuffer ??= resourceFactory.CreateBuffer(new BufferDescription(64, BufferUsage.UniformBuffer));
                material.RoughnessBuffer ??= resourceFactory.CreateBuffer(new BufferDescription(64, BufferUsage.UniformBuffer));
                material.AmbientOcclusionBuffer ??= resourceFactory.CreateBuffer(new BufferDescription(64, BufferUsage.UniformBuffer));

                // Renderer
                if (renderer.PipeLine == null || transform.WorldSet == null || camera.ResourceSet == null ||
                    material.MaterialSet == null)
                {
                    var pipelineDescription = new GraphicsPipelineDescription
                    {
                        BlendState = BlendStateDescription.SingleOverrideBlend,
                        PrimitiveTopology = PrimitiveTopology.TriangleList,
                        ResourceLayouts = new[] {_cameraSetLayout, _worldLayout, _materialLayout},

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
                        ShaderSet = new ShaderSetDescription(new[] {_sharedVertexLayout}, material.Shaders),
                        Outputs = graphicsDevice.SwapchainFramebuffer.OutputDescription
                    };

                    renderer.PipeLine ??= resourceFactory.CreateGraphicsPipeline(pipelineDescription);

                    transform.WorldSet ??= resourceFactory.CreateResourceSet(new ResourceSetDescription(
                        _worldLayout, transform.ModelBuffer));

                    material.MaterialSet ??= resourceFactory.CreateResourceSet(new ResourceSetDescription(
                        _materialLayout, 
                        material.AlbedoBuffer, 
                        material.MetallicBuffer, 
                        material.RoughnessBuffer,
                        material.AmbientOcclusionBuffer
                    ));

                    camera.ResourceSet ??= resourceFactory.CreateResourceSet(new ResourceSetDescription(
                        _cameraSetLayout,
                        camera.ProjectionBuffer,
                        camera.ViewBuffer,
                        camera.PositionBuffer));
                }
            }
        }

        public void OnUpdate(ref Context context)
        {
        }
    }
}