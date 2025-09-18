using VRageMath;

namespace ClientPlugin.Extensions
{
    public static class VectorExtensions
    {
        public static string Format(this Vector3I v)
        {
            return $"[{v.X}, {v.Y}, {v.Z}]";
        }
    }
}