using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using HarmonyLib;

namespace ClientPlugin.Extensions
{
    public static class Extensions
    {
        private static readonly FieldInfo DefaultCustomNameField = AccessTools.DeclaredField(typeof(MyTerminalBlock), "m_defaultCustomName");

        public static void GetDefaultName(this MyTerminalBlock terminalBlock, StringBuilder result)
        {
            // result.AppendStringBuilder(terminalBlock.m_defaultCustomName);
            result.AppendStringBuilder((StringBuilder)DefaultCustomNameField.GetValue(terminalBlock));
        }

        private static readonly MethodInfo GetDirectionStringMethod = AccessTools.DeclaredMethod(typeof(MyThrust), "GetDirectionString");

        public static string GetDirectionString(this MyThrust thrust, StringBuilder result)
        {
            // return thrust.GetDirectionString();
            return (string)GetDirectionStringMethod.Invoke(thrust, Array.Empty<object>());
        }

        private static readonly FieldInfo BlocksField = AccessTools.DeclaredField(typeof(MyBlockGroup), "Blocks");

        public static HashSet<MyTerminalBlock> GetBlocksField(this MyBlockGroup blockGroup)
        {
            return (HashSet<MyTerminalBlock>)BlocksField.GetValue(blockGroup);
        }
    }
}