using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text.RegularExpressions;
using ImGuiNET;
using Microsoft.Extensions.Logging;
using Trialogue.Common;
using Trialogue.Components;
using Trialogue.Ecs;
using Trialogue.Window;
using Veldrid;

namespace Trialogue.Systems.Rendering
{
    public class UiRenderSystem : IEcsStartSystem, IEcsUpdateSystem, IEcsRenderSystem, IEcsDestroySystem
    {
        private Transform _transformTest = default;

        private IDictionary<string, string> _typeNames;

        private readonly ILogger<UiRenderSystem> _log;
        private EcsWorld _world;
        
        private ImGuiRenderer imgUiRenderer;
        private ImFontPtr font;
        
        private EcsEntity _selectedEntity;

        private string _tempText;
        
        public UiRenderSystem(ILogger<UiRenderSystem> log)
        {
            _log = log;
            _typeNames = new Dictionary<string, string>();
        }

        public void OnStart(ref Context context)
        {
            var graphicsDevice = context.Window.GraphicsDevice;
            
            imgUiRenderer = new ImGuiRenderer(graphicsDevice, graphicsDevice.MainSwapchain.Framebuffer.OutputDescription, context.Window.Size.Width, context.Window.Size.Height);
            
            // Enable docking with ImGui
            ImGui.GetIO().ConfigFlags |= ImGuiConfigFlags.DpiEnableScaleFonts;
            ImGui.GetIO().ConfigFlags |= ImGuiConfigFlags.DpiEnableScaleViewports;
            ImGui.GetIO().ConfigFlags |= ImGuiConfigFlags.DockingEnable;

            var style = ImGui.GetStyle();
            
            imgUiRenderer.RecreateFontDeviceTexture();
        }
        
        public void OnUpdate(ref Context context)
        {
            imgUiRenderer.WindowResized(context.Window.Native.Width, context.Window.Native.Height);
            imgUiRenderer.Update(1f / 60f, context.Input);
            
            BuildEntities(ref context);
            BuildEntity(ref context);
        }

        public void OnRender(ref Context context)
        {
            imgUiRenderer.Render(context.Window.GraphicsDevice, context.Window.CommandList);
        }

        public void OnDestroy(ref Context context)
        {
           
        }

        void BuildEntity(ref Context context)
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
                    
                    ImGui.Text(name);
                    component.DrawUi(ref _selectedEntity);
                }

                ImGui.Spacing();
                if (ImGui.Button("Add component"))
                {
                    
                }
            }
            
            ImGui.End();
        }
        
        void BuildEntities(ref Context context)
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

            for (var index = 0; index < entities.Length; index++)
            {
                var entity = entities[index];
                if (ImGui.TreeNode(entity.Id.ToString(), entity.Get<ComponentInfo>().EntityName))
                {
                    ImGui.TreePop();
                }

                if (ImGui.IsItemClicked())
                {
                    _selectedEntity = entity;
                    _tempText = entity.Get<ComponentInfo>().EntityName;
                }
            }

            ImGui.End();
        }
    }
}