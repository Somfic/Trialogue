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
        public Quaternion Rotation;

        internal DeviceBuffer ModelBuffer;
        internal ResourceSet WorldSet;

        internal Matrix4x4 CalculateModelMatrix(ref Context context)
        {
            var transform = Matrix4x4.CreateTranslation(Position);
            var rotation = Matrix4x4.CreateFromQuaternion(Rotation);
            var scale = Matrix4x4.CreateScale(Scale);

            return scale * transform * rotation;
        }

        public void Dispose()
        {
        }
    }
}