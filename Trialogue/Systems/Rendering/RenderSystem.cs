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
    public class RenderSystem : IEcsStartSystem, IEcsRenderSystem, IEcsDestroySystem
    {
        private readonly ILogger<RenderSystem> _log;
        private EcsFilter<Camera, Transform> _cameraFilter;

        private EcsFilter<Model, Material, Transform, Renderer> _filter;

        private EcsWorld _world = null;
        private ResourceLayout _cameraSetLayout;
        private VertexLayoutDescription _sharedVertexLayout;

        public RenderSystem(ILogger<RenderSystem> log)
        {
            _log = log;
        }

        public void OnStart(ref Context context)
        {
            var graphicsDevice = context.Window.GraphicsDevice;
            var resourceFactory = graphicsDevice.ResourceFactory;

            _cameraSetLayout = resourceFactory.CreateResourceLayout(
                new ResourceLayoutDescription(
                    new ResourceLayoutElementDescription("ProjectionBuffer", ResourceKind.UniformBuffer,
                        ShaderStages.Vertex),
                    new ResourceLayoutElementDescription("ViewBuffer", ResourceKind.UniformBuffer,
                        ShaderStages.Vertex),
                    new ResourceLayoutElementDescription("PositionBuffer",
                        ResourceKind.UniformBuffer, ShaderStages.Vertex)));
            
            _sharedVertexLayout = new VertexLayoutDescription(
                new VertexElementDescription("Position", VertexElementSemantic.TextureCoordinate,
                    VertexElementFormat.Float3),
                new VertexElementDescription("Normal", VertexElementSemantic.TextureCoordinate,
                    VertexElementFormat.Float3),
                new VertexElementDescription("TexCoord", VertexElementSemantic.TextureCoordinate,
                    VertexElementFormat.Float2));
        }

        public void OnRender(ref Context context)
        {
            CreateResources(ref context);
            
            if (_cameraFilter.IsEmpty()) throw new Exception("No camera entity found");

            ref var cameraEntity = ref _cameraFilter.GetEntity(0);
            ref var camera = ref cameraEntity.Get<Camera>();
            ref var cameraTransform = ref cameraEntity.Get<Transform>();

            var projectionMatrix = camera.CalculateProjectionMatrix(ref context);
            var viewMatrix = camera.CalculateViewMatrix(ref cameraTransform);

            var graphicsDevice = context.Window.GraphicsDevice;
            var resourceFactory = graphicsDevice.ResourceFactory;
            var commandList = context.Window.CommandList;

            foreach (var i in _filter)
            {
                ref var model = ref _filter.Get1(i);
                ref var material = ref _filter.Get2(i);
                ref var transform = ref _filter.Get3(i);
                ref var renderer = ref _filter.Get4(i);

                var worldMatrix = transform.CalculateWorldMatrix(ref context);

                commandList.UpdateBuffer(camera.ProjectionBuffer, 0, ref projectionMatrix);
                commandList.UpdateBuffer(camera.ViewBuffer, 0, ref viewMatrix);
                commandList.UpdateBuffer(camera.PositionBuffer, 0, ref cameraTransform.Position);
                commandList.UpdateBuffer(transform.WorldBuffer, 0, ref worldMatrix);

                commandList.SetPipeline(renderer.PipeLine);

                foreach (var modelResource in model.Resources)
                {
                    commandList.SetVertexBuffer(0, modelResource.VertexBuffer);
                    commandList.SetIndexBuffer(modelResource.IndexBuffer, modelResource.IndexFormat);

                    commandList.SetGraphicsResourceSet(0, camera.ResourceSet);
                    commandList.SetGraphicsResourceSet(1, transform.WorldSet);

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

            if (_cameraFilter.IsEmpty()) throw new Exception("No camera entity found");

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
                transform.WorldBuffer ??= 
                    resourceFactory.CreateBuffer(new BufferDescription(64, BufferUsage.UniformBuffer));
                
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

                // Renderer
                if (renderer.PipeLine == null || transform.WorldSet == null || camera.ResourceSet == null)
                {
                    var worldLayout = resourceFactory.CreateResourceLayout(
                        new ResourceLayoutDescription(new ResourceLayoutElementDescription("WorldBuffer",
                            ResourceKind.UniformBuffer, ShaderStages.Vertex)));

                    var pipelineDescription = new GraphicsPipelineDescription
                    {
                        BlendState = BlendStateDescription.SingleOverrideBlend,
                        PrimitiveTopology = PrimitiveTopology.TriangleList,
                        ResourceLayouts = new[] {_cameraSetLayout, worldLayout},

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
                        worldLayout, transform.WorldBuffer));

                    camera.ResourceSet ??= resourceFactory.CreateResourceSet(new ResourceSetDescription(
                        _cameraSetLayout,
                        camera.ProjectionBuffer,
                        camera.ViewBuffer,
                        camera.PositionBuffer));
                }
            }
        }
    }
}