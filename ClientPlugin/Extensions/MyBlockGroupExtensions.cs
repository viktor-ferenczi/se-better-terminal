using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.GameSystems;

namespace ClientPlugin.Extensions
{
    public static class MyBlockGroupExtensions
    {
        static readonly FieldInfo BlocksField = AccessTools.DeclaredField(typeof(MyBlockGroup), "Blocks");

        public static HashSet<MyTerminalBlock> GetBlocks(this MyBlockGroup group)
            => (HashSet<MyTerminalBlock>)BlocksField.GetValue(group);
    }
}