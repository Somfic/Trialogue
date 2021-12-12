using System;
using System.Numerics;
using ImGuiNET;
using Trialogue.Ecs;
using Trialogue.Window;
using Veldrid;

namespace Trialogue.Components
{
    public struct Camera : IEcsComponent
    {
        public bool IsOrthographic;
        public float NearPlane;
        public float FarPlane;
        public float FieldOfView;
        public float Zoom;
        public float Cone;
        public bool IsFollowingTarget;
        public Vector3 Target;

        internal DeviceBuffer ProjectionBuffer;
        internal DeviceBuffer ViewBuffer;
        internal DeviceBuffer PositionBuffer;

        internal ResourceSet ResourceSet;

        internal Matrix4x4 CalculateProjectionMatrix(ref Context context)
        {
            NearPlane = Math.Min(FarPlane, NearPlane);

            if (NearPlane == 0) NearPlane = 0.001f;

            FarPlane = Math.Max(FarPlane, NearPlane + 0.001f);

            Cone = MathF.Max(0.1f, MathF.Min(Cone, 100));
            
            FieldOfView = MathF.Max(1, MathF.Min(FieldOfView, 179));

            var proj = IsOrthographic
                ? Matrix4x4.CreateOrthographic(Cone, Cone * context.Window.Size.Height / context.Window.Size.Width,
                    NearPlane, FarPlane)
                : Matrix4x4.CreatePerspectiveFieldOfView(FieldOfView,
                    context.Window.Size.Width / (float) context.Window.Size.Height, NearPlane, FarPlane);

            return proj;
        }

        internal Matrix4x4 CalculateViewMatrix(ref Transform transform)
        {
            Matrix4x4 translation = default;

            if (IsFollowingTarget)
            {
                var forward = Vector3.Normalize(transform.Position - Target);
                var right = Vector3.Cross(Vector3.Normalize(Vector3.UnitY), forward);
                var up = Vector3.Cross(forward, right);

                translation = Matrix4x4.CreateLookAt(transform.Position, Target, up);
            }
            else
            {
                Matrix4x4.CreateTranslation(transform.Position);
            }

            Zoom = MathF.Max(1f, MathF.Min(Zoom, 100));
            var zoom = Matrix4x4.CreateScale(Zoom);

            //var rotation = Matrix4x4.CreateFromQuaternion(transform.Rotation);

            return zoom * translation;
        }

        public void DrawUi(ref EcsEntity ecsEntity)
        {
            ImGui.Checkbox("Orthographic", ref IsOrthographic);

            ImGui.DragFloat("Near plane", ref NearPlane, 0.001f, 0.001f, float.MaxValue);
            ImGui.DragFloat("Far plane", ref FarPlane, 1f, NearPlane, float.MaxValue);

            if (IsOrthographic)
                ImGui.SliderFloat("Cone size", ref Cone, 0.1f, 100f);
            else
                ImGui.SliderAngle("Field of view", ref FieldOfView, 1f, 179f);

            ImGui.SliderFloat("Zoom", ref Zoom, 1f, 100f);

            ImGui.DragFloat3("", ref Target, 0.5f);
            ImGui.SameLine();
            ImGui.Checkbox("Target", ref IsFollowingTarget);

            ecsEntity.Update(this);
        }

        public void Dispose()
        {
        }
    }
}