using System.Reflection;
using HarmonyLib;
using Sandbox.Game.Entities;

namespace ClientPlugin.Extensions
{
    public static class MyThrustExtensions
    {
        private static readonly MethodInfo GetDirectionStringMethod = AccessTools.DeclaredMethod(typeof(MyThrust), "GetDirectionString");

        public static string GetDirectionString(this MyThrust thrust)
            => (string)GetDirectionStringMethod.Invoke(thrust, null);
    }
}