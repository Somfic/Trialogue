using System;
using System.Numerics;
using ImGuiNET;
using Trialogue.Ecs;
using Trialogue.Window;
using Veldrid;

namespace Trialogue.Components
{
    public struct Transform : IEcsComponent
    {
        public Vector3 Position;
        public Vector3 Scale;
        public Vector3 Rotation;

        public DeviceBuffer ModelBuffer;
        public ResourceSet WorldSet;

        internal Matrix4x4 CalculateModelMatrix(ref Context context)
        {
            var transform = Matrix4x4.CreateTranslation(Position);

            var rotationX = Matrix4x4.CreateRotationX(Rotation.X * MathF.PI / 180f);
            var rotationY = Matrix4x4.CreateRotationY(Rotation.Y * MathF.PI / 180f);
            var rotationZ = Matrix4x4.CreateRotationZ(Rotation.Z * MathF.PI / 180f);

            var scale = Matrix4x4.CreateScale(Scale);

            return scale * transform * rotationX * rotationY * rotationZ;
        }

        public void DrawUi(ref EcsEntity ecsEntity)
        {
            ImGui.DragFloat3("Position", ref Position, 0.5f);
            ImGui.DragFloat3("Scale", ref Scale, 0.5f);

            ecsEntity.Update(this);
        }

        public void Dispose()
        {
        }
    }
}