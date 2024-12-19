using System.Diagnostics.CodeAnalysis;
using HarmonyLib;
using Sandbox.Graphics.GUI;
using VRage.Utils;
using VRageMath;

namespace ClientPlugin.Patches
{
    // ReSharper disable once UnusedType.Global
    [HarmonyPatch(typeof(MyGuiControlColor))]
    [SuppressMessage("ReSharper", "UnusedMember.Local")]
    public static class MyGuiControlColorPatch
    {
        [HarmonyPrefix]
        [HarmonyPatch(MethodType.Constructor, typeof(string), typeof(float), typeof(Vector2), typeof(Color), typeof(Color), typeof(MyStringId), typeof(bool), typeof(string), typeof(bool), typeof(bool), typeof(float))]
        private static bool ConstructorPrefix(string text)
        {
            // Run the constructor only when creating an instance of MyGuiControlColor, not of our subclass
            return text != null;
        }
    }
}