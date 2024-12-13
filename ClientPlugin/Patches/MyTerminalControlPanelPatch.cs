using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using ClientPlugin.Logic;
using ClientPlugin.Tools;
using Epic.OnlineServices.P2P;
using HarmonyLib;
using Havok;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.GameSystems;
using Sandbox.Game.Gui;
using Sandbox.Graphics.GUI;

namespace ClientPlugin.Patches
{
    [HarmonyPatch(typeof(MyTerminalControlPanel))]
    public class MyTerminalControlPanelPatch
    {
        private static ControlPanelLogic logic;

        [HarmonyPrefix]
        [HarmonyPatch("Init")]
        private static bool InitPrefix(MyTerminalControlPanel __instance, IMyGuiControlsParent controlsParent)
        {
            if (logic != null)
                logic.Close();
            
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

        [HarmonyPostfix]
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

        [HarmonyPrefix]
        [HarmonyPatch("TerminalSystem_BlockAdded")]
        private static bool TerminalSystem_BlockAddedPrefix(MyTerminalBlock obj)
        {
            logic.BlockAdded(obj);
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
        private static void ClearBlockListPostfix()
        {
            if (logic != null)
            {
                logic.Close();
                logic = null;
            }
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

            // We replace the code between these two lines:
            // this.m_blockListbox.IsInBulkInsert = true;
            // this.m_blockListbox.IsInBulkInsert = false;
            var bulkInsertSetters = il.FindAllIndex(ci => ci.opcode == OpCodes.Callvirt && ci.operand is MethodInfo mi && mi.Name == "set_IsInBulkInsert").ToArray();
            if (bulkInsertSetters.Length != 2)
                throw new Exception("Cannot find IsInBulkInsert setters in PopulateBlockList");

            // Delete the whole foreach loop
            var i = bulkInsertSetters[0] + 1;
            Debug.Assert(il[i].opcode == OpCodes.Stloc_S);
            var j = bulkInsertSetters[1];
            while (il[j].opcode != OpCodes.Ldarg_0) j--;
            il.RemoveRange(i, j - i);

            // After the IsInBulkInsert = true line the top of stack contains MyTerminalBlock[]
            var methodInfo = AccessTools.DeclaredMethod(typeof(MyTerminalControlPanelPatch), nameof(PopulateBlockList_AddBlocks));
            il.Insert(i, new CodeInstruction(OpCodes.Call, methodInfo));

            il.RecordPatchedCode();
            return il;
        }

        private static void PopulateBlockList_AddBlocks(MyTerminalBlock[] blocks)
        {
            logic.PopulateBlockList_AddBlocks(blocks);
        }

        [HarmonyTranspiler]
        [HarmonyPatch("UpdateItemAppearance")]
        private static IEnumerable<CodeInstruction> UpdateItemAppearanceTranspiler(IEnumerable<CodeInstruction> code)
        {
            var il = code.ToList();
            il.RecordOriginalCode();

            // Remove the first two statements:
            // item.Text.Clear();
            // block.GetTerminalName(item.Text);
            var count = il.FindAllIndex(ci => ci.opcode == OpCodes.Ldarg_1)[1];
            il.RemoveRange(0, count);

            // Inject a call
            var i = 0;
            il.Insert(i++, new CodeInstruction(OpCodes.Ldarg_1)); // block
            il.Insert(i++, new CodeInstruction(OpCodes.Ldarg_2)); // item
            var methodInfo = AccessTools.DeclaredMethod(typeof(MyTerminalControlPanelPatch), nameof(UpdateItemAppearance_ShowDefaultNameFilter));
            il.Insert(i, new CodeInstruction(OpCodes.Call, methodInfo));

            il.RecordPatchedCode();
            return il;
        }

        private static void UpdateItemAppearance_ShowDefaultNameFilter(MyTerminalBlock block, MyGuiControlListbox.Item item)
        {
            logic.UpdateItemAppearance_ShowDefaultNameFilter(block, item);
        }

        #endregion
    }
}