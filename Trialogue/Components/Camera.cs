using System;
using System.ComponentModel.DataAnnotations;
using System.Numerics;
using ImGuiNET;
using Trialogue.Ecs;
using Trialogue.Systems.Rendering;
using Trialogue.Window;
using Veldrid;

namespace Trialogue.Components
{
    public struct Camera : IEcsComponent
    {
        public bool IsOrthographic;
        
        [Range(0.0001, 10000.0)]
        public float NearPlane;
        
        [Range(0.0001, 10000.0)]
        public float FarPlane;
        
        [Range(1, 179)]
        public float FieldOfView;
        
        [Range(0, 100)]
        public float Zoom;
        
        [Range(0, 100)]
        public float Cone;
        
        public bool IsFollowingTarget;
        
        public Vector3 Target;
        
        internal Uniform<Matrix4x4> ProjectionUniform;
        internal Uniform<Matrix4x4> ViewUniform;
        internal UniformSet UniformSet;

        internal Matrix4x4 CalculateProjectionMatrix(ref Context context)
        {
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

        public void Dispose()
        {
        }
    }
}