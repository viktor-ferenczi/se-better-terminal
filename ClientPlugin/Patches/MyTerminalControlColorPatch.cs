using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using ClientPlugin.Logic;
using ClientPlugin.Tools;
using HarmonyLib;
using Sandbox.Game.Gui;
using Sandbox.Graphics.GUI;

namespace ClientPlugin.Patches
{
    // ReSharper disable once UnusedType.Global
    [HarmonyPatch(typeof(MyTerminalControlColor<>))]
    [SuppressMessage("ReSharper", "UnusedMember.Local")]
    public static class MyTerminalControlColorPatch
    {
        [HarmonyTranspiler]
        [HarmonyPatch("CreateGui")]
        private static IEnumerable<CodeInstruction> CreateGuiTranspiler(IEnumerable<CodeInstruction> code)
        {
            var il = code.ToList();
            il.RecordOriginalCode();

            il = il.ReplaceType(
                typeof(MyGuiControlColor), 
                typeof(MyGuiControlColorHex),
                new Dictionary<string, string>
                {
                    {"add_OnChange", "add_MyGuiControlColorHex_OnChange"},
                }
                ).ToList();

            il.RecordPatchedCode();
            return il;
        }

        [HarmonyTranspiler]
        [HarmonyPatch("OnUpdateVisual")]
        private static IEnumerable<CodeInstruction> OnUpdateVisualTranspiler(IEnumerable<CodeInstruction> code)
        {
            var il = code.ToList();
            il.RecordOriginalCode();

            il = il.ReplaceType(typeof(MyGuiControlColor), typeof(MyGuiControlColorHex)).ToList();

            il.RecordPatchedCode();
            return il;
        }
    }
}