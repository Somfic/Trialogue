using System.Numerics;
using System.Runtime.CompilerServices;
using aiMatrix4x4 = Assimp.Matrix4x4;

namespace Trialogue.Importer
{
    public static class AssimpExtensions
    {
        public static unsafe System.Numerics.Matrix4x4 ToSystemMatrixTransposed(this aiMatrix4x4 mat)
        {
            return System.Numerics.Matrix4x4.Transpose(Unsafe.Read<System.Numerics.Matrix4x4>(&mat));
        }

        public static System.Numerics.Quaternion ToSystemQuaternion(this Assimp.Quaternion quat)
        {
            return new System.Numerics.Quaternion(quat.X, quat.Y, quat.Z, quat.W);
        }

        public static Vector3 ToSystemVector3(this Assimp.Vector3D v3)
        {
            return new Vector3(v3.X, v3.Y, v3.Z);
        }
    }
}