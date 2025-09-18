using System.Runtime.CompilerServices;
using Sandbox.Game.Entities;

namespace ClientPlugin.Extensions
{
    public static class MyCubeGridExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string GetSafeName(this MyCubeGrid grid)
        {
            return grid?.DisplayNameText ?? grid?.DisplayName ?? grid?.Name ?? "";
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string GetDebugName(this MyCubeGrid grid)
        {
            return $"{grid.GetSafeName()} [{grid.EntityId}]";
        }
    }
}