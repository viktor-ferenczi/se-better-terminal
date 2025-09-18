using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using ClientPlugin.Logic;
using ClientPlugin.Tools;
using HarmonyLib;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.GameSystems;
using Sandbox.Game.Gui;
using Sandbox.Graphics.GUI;

namespace ClientPlugin.Patches
{
    // ReSharper disable once UnusedType.Global
    [HarmonyPatch(typeof(MyTerminalControlPanel))]
    [SuppressMessage("ReSharper", "UnusedMember.Local")]
    public static class MyTerminalControlPanelPatch
    {
        private static ControlPanelLogic logic;

        // ReSharper disable once InconsistentNaming
        [HarmonyPrefix]
        [HarmonyPatch(nameof(MyTerminalControlPanel.Init))]
        private static bool InitPrefix(MyTerminalControlPanel __instance, IMyGuiControlsParent controlsParent)
        {
            Debug.Assert(__instance != null);
            
            // Config conditions are inside the constructor
            logic = new ControlPanelLogic(__instance, controlsParent);
            return true;
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(MyTerminalControlPanel.Init))]
        private static void InitPostfix()
        {
            if (Config.Current.EnableBlockFilter)
            {
                logic.ScrollBlockListToTop();
            }

            logic.SetSearchText(Config.Current.DefaultSearchText);
        }

        #region Groups

        [HarmonyPostfix]
        [HarmonyPatch("AddGroupToList")]
        private static void AddGroupToListPostfix(MyBlockGroup group)
        {
            if (!Config.Current.EnableBlockFilter)
                return;

            logic.RegisterGroup(group);
        }

        [HarmonyPostfix]
        [HarmonyPatch("TerminalSystem_GroupRemoved")]
        private static void TerminalSystem_GroupRemovedPostfix(MyBlockGroup group)
        {
            if (!Config.Current.EnableBlockFilter)
                return;

            logic.UnregisterGroup(group);
        }

        [HarmonyPostfix]
        [HarmonyPatch("groupDelete_ButtonClicked")]
        private static void groupDelete_ButtonClickedPostfix()
        {
            if (!Config.Current.EnableBlockFilter)
                return;

            logic.UpdateModeSelector();
        }

        [HarmonyPostfix]
        [HarmonyPatch("groupSave_ButtonClicked")]
        private static void groupSave_ButtonClickedPostfix()
        {
            if (!Config.Current.EnableBlockFilter)
                return;

            logic.PrepareGroupRenaming();
            logic.UpdateModeSelector();
        }

        [HarmonyPostfix]
        [HarmonyPatch("SelectBlocks", new Type[0])]
        private static void SelectBlocksPostfix()
        {
            if (!Config.Current.EnableBlockFilter)
                return;

            logic.AfterSelectBlocks();
        }

        #endregion Groups

        #region Blocks

        [HarmonyPrefix]
        [HarmonyPatch(nameof(MyTerminalControlPanel.TerminalSystem_BlockAdded))]
        private static bool TerminalSystem_BlockAddedPrefix(MyTerminalBlock obj)
        {
            if (!Config.Current.EnableBlockFilter)
                return true;

            logic.BlockAdded(obj);
            return false;
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(MyTerminalControlPanel.TerminalSystem_BlockRemoved))]
        private static void TerminalSystem_BlockRemovedPostfix(MyTerminalBlock obj)
        {
            if (!Config.Current.EnableBlockFilter)
                return;

            logic.UnregisterBlock(obj);
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(MyTerminalControlPanel.Close))]
        private static bool ClosePrefix()
        {
            if (!Config.Current.EnableBlockFilter && logic != null)
            {
                logic.Close();
                logic = null;
            }
            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(nameof(MyTerminalControlPanel.blockSearch_TextChanged), typeof(string), typeof(bool))]
        private static bool blockSearch_TextChangedPrefix(string text, bool scrollToTop)
        {
            if (!Config.Current.EnableBlockFilter)
                return true;

            logic.blockSearch_TextChanged(text, scrollToTop);
            return false;
        }

        [HarmonyTranspiler]
        [HarmonyPatch(nameof(MyTerminalControlPanel.PopulateBlockList))]
        private static IEnumerable<CodeInstruction> PopulateBlockListTranspiler(IEnumerable<CodeInstruction> code)
        {
            var il = code.ToList();
            il.RecordOriginalCode();
            il.VerifyCodeHash("60c4cb3b");

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
            // Config condition is inside
            logic.PopulateBlockList_AddBlocks(blocks);

            // Must update the mode selector, but it should keep its current selection
            logic.UpdateModeSelector();
        }

        [HarmonyTranspiler]
        [HarmonyPatch(nameof(MyTerminalControlPanel.UpdateItemAppearance))]
        private static IEnumerable<CodeInstruction> UpdateItemAppearanceTranspiler(IEnumerable<CodeInstruction> code)
        {
            var il = code.ToList();
            il.RecordOriginalCode();
            il.VerifyCodeHash("378bbed9");
            
            // Replace this statement:
            // block.GetTerminalName(item.Text);
            var blockLoads = il.FindAllIndex(ci => ci.opcode == OpCodes.Ldarg_1).ToArray();
            Debug.Assert(blockLoads.Length >= 2);
            var i = blockLoads[0]; // block.GetTerminalName(item.Text);
            var j = blockLoads[1]; // if (!block.IsFunctional)
            il.RemoveRange(i, j - i);

            // Replace with logic to allow showing the default block names instead of the player defined ones
            il.Insert(i++, new CodeInstruction(OpCodes.Ldarg_1)); // block
            il.Insert(i++, new CodeInstruction(OpCodes.Ldarg_2)); // item
            var methodInfo = AccessTools.DeclaredMethod(typeof(MyTerminalControlPanelPatch), nameof(UpdateItemAppearance_DefaultNameImplementation));
            il.Insert(i, new CodeInstruction(OpCodes.Call, methodInfo));

            il.RecordPatchedCode();
            return il;
        }

        private static void UpdateItemAppearance_DefaultNameImplementation(MyTerminalBlock block, MyGuiControlListbox.Item item)
        {
            // Config condition is inside
            logic.UpdateItemAppearance_DefaultNameImplementation(block, item);
        }

        #endregion
    }
}