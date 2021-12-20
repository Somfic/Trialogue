using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text.RegularExpressions;
using ImGuiNET;
using Microsoft.Extensions.Logging;
using Trialogue.Components;
using Trialogue.Ecs;
using Trialogue.Systems.Rendering.Ui;
using Trialogue.Window;
using Veldrid;

namespace Trialogue.Systems.Rendering
{
    public class UiRenderSystem : IEcsStartSystem, IEcsUpdateSystem, IEcsRenderSystem, IEcsDestroySystem
    {
        private readonly ILogger<UiRenderSystem> _log;

        private EcsWorld _world = null;
        private EcsEntity _selectedEntity;

        private readonly IDictionary<string, string> _typeNames;

        private ImGuiRenderer _imgUiRenderer;

        private IList<Type> components = new List<Type>();

        public UiRenderSystem(ILogger<UiRenderSystem> log)
        {
            _log = log;
            _typeNames = new Dictionary<string, string>();
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

            BuildHierarchy(ref context);
            BuildInspector(ref context);
        }

        public void OnRender(ref Context context)
        {
            _imgUiRenderer.Render(context.Window.GraphicsDevice, context.Window.CommandList);
        }

        public void OnDestroy(ref Context context)
        {
            _typeNames.Clear();
            _imgUiRenderer.Dispose();
            components.Clear();
        }

        private void BuildHierarchy(ref Context context)
        {
            ImGui.SetNextWindowPos(new Vector2(0, 0));
            ImGui.SetNextWindowSize(new Vector2(150, context.Window.Size.Height));

            ImGui.Begin("Hierarchy", ImGuiWindowFlags.NoCollapse);
            var style = ImGui.GetStyle();
            style.WindowTitleAlign = new Vector2(0.5f, 0.5f);
            style.FramePadding = new Vector2(8, 4);
            style.FrameRounding = 0;
            style.ChildRounding = 0;

            var selected = 0;

            if (ImGui.Button("New Entity"))
            {
                _selectedEntity = _world.NewEntity("Empty entity");
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

        private void BuildInspector(ref Context context)
        {
            ImGui.SetNextWindowPos(new Vector2(context.Window.Size.Width - 350, 0));
            ImGui.SetNextWindowSize(new Vector2(350, context.Window.Size.Height));

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
                    DrawComponent(component);
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

                                _log.LogInformation("Adding {Component} to {Entity}", component.Name,
                                    _selectedEntity.Get<ComponentInfo>().EntityName);

                                addGenericMethod.Invoke(_selectedEntity, Array.Empty<object>());
                            }
                        }
                    }
                }

                ImGui.EndCombo();
            }

            ImGui.End();
        }

        private void DrawComponent(IEcsComponent component)
        {
            var fields = GetUiFields(component.GetType());
            var componentName = GetTypeName(component.GetType());
            var header = DrawHeader(componentName, fields.Length > 0);

            if (componentName != "Component Info")
            {
                var isDeleted = DrawDeleteButton();
                if (isDeleted)
                {
                    var deleteMethod = _selectedEntity.GetType().GetMethod("Del");
#if DEBUG
                    if (deleteMethod == null)
                    {
                        throw new Exception("Could not find Del method");
                    }
#endif
                    var deleteGenericMethod = deleteMethod.MakeGenericMethod(component.GetType());

                    _log.LogInformation("Deleting {Component} from {Entity}", component.GetType().Name,
                        _selectedEntity.Get<ComponentInfo>().EntityName);

                    deleteGenericMethod.Invoke(_selectedEntity, Array.Empty<object>());

                    return;
                }
            }

            if (header)
            {
                ImGui.BeginGroup();

                foreach (var field in fields)
                {
                    ImGui.PushID(field.GetType().FullName);
                    DrawField(field, component);
                    ImGui.PopID();
                }

                var updateMethod = _selectedEntity.GetType().GetMethod("Update");
#if DEBUG
                if (updateMethod == null)
                {
                    throw new Exception("Could not find Update method");
                }
#endif
                var addGenericMethod = updateMethod.MakeGenericMethod(component.GetType());

                addGenericMethod.Invoke(_selectedEntity, new object[] {component});

                ImGui.EndGroup();
                ImGui.NewLine();
            }
        }

        private void DrawField(FieldInfo field, object instance)
        {
            string name = GetPrettyName(field.Name);

            if (field.FieldType.IsPrimitive)
            {
                // Switch case for primitive types
                switch (Type.GetTypeCode(field.FieldType))
                {
                    case TypeCode.Int32:
                    {
                        int value = (int) field.GetValue(instance);
                        ImGui.InputInt(name, ref value);
                        field.SetValue(instance, value);
                        break;
                    }

                    case TypeCode.Decimal:
                    case TypeCode.Double:
                    case TypeCode.Single:
                    {
                        float value = (float) field.GetValue(instance);

                        if (Attribute.IsDefined(field, typeof(RangeAttribute)))
                        {
                            // Get the range attribute
                            var range = Attribute.GetCustomAttribute(field, typeof(RangeAttribute)) as RangeAttribute;

                            float min = 0;
                            float max = 0;

                            if (range.Minimum is double)
                            {
                                min = (float) (double) range.Minimum;
                                max = (float) (double) range.Maximum;
                            }
                            else if (range.Minimum is int)
                            {
                                min = (float) (int) range.Minimum;
                                max = (float) (int) range.Maximum;
                            }

                            ImGui.SliderFloat(name, ref value, (float) min, (float) max);
                        }
                        else
                        {
                            ImGui.DragFloat(name, ref value);
                        }

                        field.SetValue(instance, value);
                        break;
                    }

                    case TypeCode.Boolean:
                    {
                        bool value = (bool) field.GetValue(instance);
                        ImGui.Checkbox(name, ref value);
                        field.SetValue(instance, value);
                        break;
                    }

                    case TypeCode.String:
                    {
                        string value = (string) field.GetValue(instance);
                        ImGui.InputText(name, ref value, 256);
                        field.SetValue(instance, value);
                        break;
                    }
                    default:
                    {
                        ImGui.Text($"Unsupported primitive type: {field.FieldType.Name}");
                        break;
                    }
                }
            }
            else if (field.FieldType.IsEnum)
            {
                var value = (int) field.GetValue(instance);
                if (ImGui.BeginCombo(name, field.FieldType.GetEnumNames()[value]))
                {
                    for (var i = 0; i < field.FieldType.GetEnumNames().Length; i++)
                    {
                        if (ImGui.Selectable(field.FieldType.GetEnumNames()[i]))
                        {
                            field.SetValue(instance, i);
                        }
                    }

                    ImGui.EndCombo();
                }
            }
            else if (field.FieldType == typeof(string))
            {
                var value = (string) field.GetValue(instance);
                ImGui.InputText(name, ref value, 256);
                field.SetValue(instance, value);
            }
            else if (field.FieldType == typeof(Vector2))
            {
                var value = (Vector2) field.GetValue(instance);
                ImGui.DragFloat2(name, ref value);
                field.SetValue(instance, value);
            }
            else if (field.FieldType == typeof(Vector3))
            {
                var value = (Vector3) field.GetValue(instance);

                if (Attribute.IsDefined(field, typeof(ColorAttribute)))
                {
                    ImGui.ColorEdit3(name, ref value);
                }
                else
                {
                    ImGui.DragFloat3(name, ref value);
                }

                field.SetValue(instance, value);
            }
            else if (field.FieldType == typeof(Vector4))
            {
                var value = (Vector4) field.GetValue(instance);
                if (Attribute.IsDefined(field, typeof(ColorAttribute)))
                {
                    ImGui.ColorEdit4(name, ref value);
                }
                else
                {
                    ImGui.DragFloat4(name, ref value);
                }

                field.SetValue(instance, value);
            }
            else
            {
                var childFieldInstance = field.GetValue(instance);

                if (childFieldInstance != null)
                {
                    var childFields = GetUiFields(childFieldInstance.GetType());


                    ImGui.Indent();


                    bool header = ImGui.CollapsingHeader(name, ImGuiTreeNodeFlags.AllowItemOverlap);
                    
                    if (DrawDeleteButton())
                    {
                        _log.LogInformation("Resetting {Name}", name);
                        field.SetValue(instance, null);
                    }
                    else
                    {
                        if(header)
                        {
                            foreach (var childField in childFields)
                            {
                                ImGui.PushID(childField.GetType().FullName);
                                DrawField(childField, childFieldInstance);
                                ImGui.PopID();
                            }
                        }   
                    }

                    ImGui.Unindent();
                }
                else
                {
                    if (field.FieldType.IsAbstract)
                    {
                        // All all types that can be created by reflection
                        var types = AppDomain.CurrentDomain.GetAssemblies()
                            .SelectMany(s => s.GetTypes())
                            .Where(p => !p.IsAbstract && field.FieldType.IsAssignableFrom(p));

                        if (ImGui.BeginCombo(name, "Instantiate field"))
                        {
                            var applicableTypes = types; //.Where(x => x.GetConstructor(Type.EmptyTypes) != null);

                            if (!applicableTypes.Any())
                            {
                                ImGui.Selectable("No applicable types");
                            }
                            else
                            {
                                foreach (var type in applicableTypes)
                                {
                                    if (ImGui.Selectable(type.Name))
                                    {
                                        field.SetValue(instance, Activator.CreateInstance(type));
                                    }
                                }
                            }

                            ImGui.EndCombo();
                        }
                    }
                }
            }
        }

        private FieldInfo[] GetUiFields(Type type)
        {
            return type.GetFields(BindingFlags.Public | BindingFlags.Instance);
        }

        private string GetTypeName(MemberInfo type)
        {
            string name;
            if (_typeNames.ContainsKey(type.Name))
            {
                name = _typeNames[type.Name];
            }
            else
            {
                name = GetPrettyName(type.Name);
                _typeNames.Add(type.Name, name);
            }

            return name;
        }

        private string GetPrettyName(string name)
        {
            return Regex.Replace(name, @"((?<=\p{Ll})\p{Lu})|((?!\A)\p{Lu}(?>\p{Ll}))", " $0");
        }

        private bool DrawHeader(string name, bool canExpand)
        {
            ImGui.AlignTextToFramePadding();

            if (canExpand)
            {
                return ImGui.CollapsingHeader(name,
                    ImGuiTreeNodeFlags.DefaultOpen | ImGuiTreeNodeFlags.AllowItemOverlap);
            }

            return ImGui.CollapsingHeader(name, ImGuiTreeNodeFlags.AllowItemOverlap);
        }

        private bool DrawDeleteButton()
        {
            ImGui.SameLine(ImGui.GetWindowWidth() - 28);
            return ImGui.Button("X");
        }
    }
}