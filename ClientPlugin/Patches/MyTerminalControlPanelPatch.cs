using System.Collections.Generic;
using System.Linq;
using ClientPlugin.Logic;
using ClientPlugin.Tools;
using HarmonyLib;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.GameSystems;
using Sandbox.Graphics.GUI;

namespace ClientPlugin.Patches
{
    [HarmonyPatch(typeof(MyTerminalControlPanel))]
    public class MyTerminalControlPanelPatch
    {
        private static ControlPanelLogic logic;

        [HarmonyPrefix]
        [HarmonyPatch("Init")]
        private static bool InitPrefix(object __instance, IMyGuiControlsParent controlsParent)
        {
            logic = new ControlPanelLogic(__instance, controlsParent);
            return true;
        }

        [HarmonyPostfix]
        [HarmonyPatch("Init")]
        private static void InitPostfix()
        {
            logic.UpdateModeSelector();
        }

        #region Groups

        [HarmonyTranspiler]
        [HarmonyPatch("AddGroupToList")]
        private static void AddGroupToListPostfix(MyBlockGroup group)
        {
            logic.RegisterGroup(group);
        }

        [HarmonyPostfix]
        [HarmonyPatch("TerminalSystem_GroupRemoved")]
        private static void TerminalSystem_GroupRemovedPostfix(MyBlockGroup group)
        {
            logic.UnregisterGroup(group);
        }

        [HarmonyPostfix]
        [HarmonyPatch("groupDelete_ButtonClicked")]
        private static void groupDelete_ButtonClickedPostfix(MyGuiControlButton obj)
        {
            logic.UpdateModeSelector();
        }

        [HarmonyPostfix]
        [HarmonyPatch("groupSave_ButtonClicked")]
        private static void groupSave_ButtonClickedPostfix(MyGuiControlButton obj)
        {
            logic.UpdateModeSelector();
        }

        #endregion Groups

        #region Blocks

        [HarmonyPostfix]
        [HarmonyPatch("TerminalSystem_BlockAdded")]
        private static bool TerminalSystem_BlockAddedPrefix(MyTerminalBlock myTerminalBlock)
        {
            logic.BlockAdded(myTerminalBlock);
            return false;
        }

        [HarmonyPostfix]
        [HarmonyPatch("TerminalSystem_BlockRemoved")]
        private static void TerminalSystem_BlockRemovedPostfix(MyTerminalBlock obj)
        {
            logic.UnregisterBlock(obj);
        }

        [HarmonyPostfix]
        [HarmonyPatch("ClearBlockList")]
        private static void ClearBlockListPostfix(MyTerminalBlock obj)
        {
            logic = null;
        }

        [HarmonyPrefix]
        [HarmonyPatch("blockSearch_TextChanged")]
        private static bool blockSearch_TextChangedPrefix(string text)
        {
            logic.blockSearch_TextChanged(text);
            return false;
        }

        [HarmonyTranspiler]
        [HarmonyPatch("PopulateBlockList")]
        private static IEnumerable<CodeInstruction> PopulateBlockListTranspiler(IEnumerable<CodeInstruction> code)
        {
            var il = code.ToList();
            il.RecordOriginalCode();

            il.RecordPatchedCode();
            return il;
        }

        [HarmonyTranspiler]
        [HarmonyPatch("UpdateItemAppearance")]
        private static IEnumerable<CodeInstruction> UpdateItemAppearanceTranspiler(IEnumerable<CodeInstruction> code)
        {
            var il = code.ToList();
            il.RecordOriginalCode();

            il.RecordPatchedCode();
            return il;
        }

        #endregion
    }
}