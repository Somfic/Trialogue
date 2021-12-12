using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text.RegularExpressions;
using ImGuiNET;
using Microsoft.Extensions.Logging;
using Trialogue.Components;
using Trialogue.Ecs;
using Trialogue.Window;
using Veldrid;

namespace Trialogue.Systems.Rendering.Ui
{
    public class UiRenderSystem : IEcsStartSystem, IEcsUpdateSystem, IEcsRenderSystem, IEcsDestroySystem
    {
        private readonly ILogger<UiRenderSystem> _log;

        private EcsWorld _world = null;
        private EcsEntity _selectedEntity;

        private readonly IDictionary<string, string> _typeNames;

        private ImGuiRenderer _imgUiRenderer;

        private IList<Type> components = new List<Type>();
        private Type selectedComponent;

        public UiRenderSystem(ILogger<UiRenderSystem> log)
        {
            _log = log;
            _typeNames = new Dictionary<string, string>();
        }

        public void OnDestroy(ref Context context)
        {
        }

        public void OnRender(ref Context context)
        {
            _imgUiRenderer.Render(context.Window.GraphicsDevice, context.Window.CommandList);
        }

        public void OnStart(ref Context context)
        {
            var graphicsDevice = context.Window.GraphicsDevice;

            _imgUiRenderer = new ImGuiRenderer(graphicsDevice,
                graphicsDevice.MainSwapchain.Framebuffer.OutputDescription, context.Window.Size.Width,
                context.Window.Size.Height);

            // Enable docking with ImGui
            ImGui.GetIO().ConfigFlags |= ImGuiConfigFlags.DpiEnableScaleFonts;
            ImGui.GetIO().ConfigFlags |= ImGuiConfigFlags.DpiEnableScaleViewports;
            ImGui.GetIO().ConfigFlags |= ImGuiConfigFlags.DockingEnable;

            var style = ImGui.GetStyle();

            _imgUiRenderer.RecreateFontDeviceTexture();

            // Add all component types to the list
            foreach (var type in Assembly.GetExecutingAssembly().GetTypes())
            {
                if (type.IsAssignableTo(typeof(IEcsComponent)) && !type.IsAbstract)
                {
                    components.Add(type);
                }
            }
        }

        public void OnUpdate(ref Context context)
        {
            _imgUiRenderer.WindowResized(context.Window.Native.Width, context.Window.Native.Height);
            _imgUiRenderer.Update(1f / 60f, context.Input);

            BuildEntities(ref context);
            BuildEntity(ref context);
        }

        private void BuildEntity(ref Context context)
        {
            ImGui.SetNextWindowPos(new Vector2(context.Window.Size.Width - 250, 0));
            ImGui.SetNextWindowSize(new Vector2(250, context.Window.Size.Height));

            ImGui.Begin("Inspector", ImGuiWindowFlags.NoCollapse);

            var style = ImGui.GetStyle();
            style.WindowTitleAlign = new Vector2(0.5f, 0.5f);
            style.FramePadding = new Vector2(8, 4);
            style.FrameRounding = 0;
            style.ChildRounding = 0;

            if (_selectedEntity != default)
            {
                foreach (var component in _selectedEntity.Components)
                {
                    ImGui.PushID(component.GetType().FullName);
                    
                    var type = component.GetType().Name;
                    var name = "";
                    if (_typeNames.ContainsKey(type))
                    {
                        name = _typeNames[type];
                    }
                    else
                    {
                        name = Regex.Replace(type, @"((?<=\p{Ll})\p{Lu})|((?!\A)\p{Lu}(?>\p{Ll}))", " $0");
                        _typeNames.Add(type, name);
                    }
                    
                    ImGui.AlignTextToFramePadding();
                    bool header = ImGui.CollapsingHeader(name, ImGuiTreeNodeFlags.DefaultOpen | ImGuiTreeNodeFlags.AllowItemOverlap);

                    if (name != "Component Info")
                    {

                        ImGui.SameLine(ImGui.GetWindowWidth() - 28);
                        if (ImGui.Button("X"))
                        {
                            var deleteMethod = _selectedEntity.GetType().GetMethod("Del");
#if DEBUG
                            if (deleteMethod == null)
                            {
                                throw new Exception("Could not find Del method");
                            }
#endif
                            var deleteGenericMethod = deleteMethod.MakeGenericMethod(component.GetType());

                            _log.LogInformation("Deleting {Component} from {Entity}", component.GetType().Name, _selectedEntity.Get<ComponentInfo>().EntityName);

                            deleteGenericMethod.Invoke(_selectedEntity, Array.Empty<object>());
                        }
                    }
                    
                    if (header)
                    {
                        ImGui.BeginGroup();
                        component.DrawUi(ref _selectedEntity);
                        ImGui.EndGroup();
                    }

                    ImGui.PopID();
                }

                ImGui.SetNextItemWidth(240);
                if (ImGui.BeginCombo("", "Add a component"))
                {
                    foreach (var component in components)
                    {
                        if (!_selectedEntity.Components.Select(x => x.GetType().FullName).Contains(component.FullName))
                        {
                            if (ImGui.Selectable(component.Name))
                            {
                                var addMethod = _selectedEntity.GetType().GetMethod("Get");
#if DEBUG
                                if (addMethod == null)
                                {
                                    throw new Exception("Could not find Get method");
                                }                          
#endif
                                var addGenericMethod = addMethod.MakeGenericMethod(component);
                                
                                _log.LogInformation("Adding {Component} to {Entity}",  component.Name, _selectedEntity.Get<ComponentInfo>().EntityName);
                                
                                addGenericMethod.Invoke(_selectedEntity, Array.Empty<object>());
                            }
                        }
                    }
                }

                ImGui.EndCombo();
            }

            ImGui.End();
        }

        private void BuildEntities(ref Context context)
        {
            ImGui.SetNextWindowPos(new Vector2(0, 0));
            ImGui.SetNextWindowSize(new Vector2(250, context.Window.Size.Height));

            ImGui.Begin("Hierarchy", ImGuiWindowFlags.NoCollapse);
            var style = ImGui.GetStyle();
            style.WindowTitleAlign = new Vector2(0.5f, 0.5f);
            style.FramePadding = new Vector2(8, 4);
            style.FrameRounding = 0;
            style.ChildRounding = 0;

            var selected = 0;

            if (ImGui.Button("New Entity"))
            {
                var entity = _world.NewEntity("Empty entity");
            }

            var entities = Array.Empty<EcsEntity>();
            _world.GetAllEntities(ref entities);

            foreach (var entity in entities)
            {
                if (ImGui.TreeNode(entity.Id.ToString(), entity.Get<ComponentInfo>().EntityName)) ImGui.TreePop();

                if (ImGui.IsItemClicked())
                {
                    _selectedEntity = entity;
                }
            }

            ImGui.End();
        }
    }
}