using System.Numerics;
using System.Runtime.CompilerServices;
using Assimp;
using aiMatrix4x4 = Assimp.Matrix4x4;
using Matrix4x4 = System.Numerics.Matrix4x4;
using Quaternion = System.Numerics.Quaternion;

namespace Trialogue.Importer
{
    public static class AssimpExtensions
    {
        public static unsafe Matrix4x4 ToSystemMatrixTransposed(this aiMatrix4x4 mat)
        {
            return Matrix4x4.Transpose(Unsafe.Read<Matrix4x4>(&mat));
        }

        public static Quaternion ToSystemQuaternion(this Assimp.Quaternion quat)
        {
            return new Quaternion(quat.X, quat.Y, quat.Z, quat.W);
        }

        public static Vector3 ToSystemVector3(this Vector3D v3)
        {
            return new Vector3(v3.X, v3.Y, v3.Z);
        }
    }
}