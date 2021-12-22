using System;
using System.ComponentModel.DataAnnotations;
using System.Numerics;
using ImGuiNET;
using Trialogue.Ecs;
using Trialogue.Systems.Rendering;
using Trialogue.Systems.Rendering.Ui;
using Trialogue.Window;
using Veldrid;

namespace Trialogue.Components
{
    public struct Light : IEcsComponent
    {
        public LightType Type;

        [Range(0f, 1000f)]
        public float Strength;

        [Color]
        public Vector3 Color;
        
        internal Uniform<float> StrengthUniform;
        internal Uniform<Vector3> ColorUniform;
        internal UniformSet UniformSet;

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