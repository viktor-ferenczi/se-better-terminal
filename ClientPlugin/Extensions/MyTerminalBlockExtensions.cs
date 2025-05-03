using System.Reflection;
using System.Text;
using HarmonyLib;
using Sandbox.Game.Entities.Cube;

namespace ClientPlugin.Extensions
{
    public static class MyTerminalBlockExtensions
    {
        private static readonly FieldInfo DefaultCustomNameField = AccessTools.DeclaredField(typeof(MyTerminalBlock), "m_defaultCustomName");

        public static StringBuilder GetDefaultCustomName(this MyTerminalBlock block)
            => (StringBuilder)DefaultCustomNameField.GetValue(block);
    }
}