using System;
using System.Numerics;
using ImGuiNET;
using Trialogue.Ecs;
using Trialogue.Window;
using Veldrid;

namespace Trialogue.Components
{
    public struct Light : IEcsComponent
    {
        public LightType Type;

        public Vector3 Color;


        public void DrawUi(ref EcsEntity ecsEntity)
        {
            if(ImGui.BeginCombo("Type", Type.ToString())) {
                foreach(LightType type in Enum.GetValues(typeof(LightType))) {
                    if(ImGui.Selectable(type.ToString())) {
                        Type = type;
                    }
                }

                ImGui.EndCombo();
            }

            ImGui.ColorEdit3("Color", ref Color);

            ecsEntity.Update(this);
        }

        public void Dispose()
        {
        }

        public enum LightType {
             Directional, 
             Point, 
             Spot 
        }
    }
}