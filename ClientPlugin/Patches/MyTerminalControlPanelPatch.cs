using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using ClientPlugin.Extensions;
using ClientPlugin.Logic;
using ClientPlugin.Tools;
using HarmonyLib;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.GameSystems;
using Sandbox.Graphics.GUI;

namespace ClientPlugin.Patches
{
    // This does not work: [HarmonyPatch("Sandbox.Game.Gui.MyTerminalControlPanel")]
    [SuppressMessage("ReSharper", "UnusedMember.Local")]
    // ReSharper disable once UnusedType.Global
    public static class MyTerminalControlPanelPatch
    {
        private static readonly bool DisableCodeValidations = (Environment.GetEnvironmentVariable("SE_PLUGIN_DISABLE_METHOD_VERIFICATION") ?? "0") != "0";
        
        // HarmonyLib does not look for a TargetType method either to return the type to patch.
        // So we have to call this ugly manual patch application from the plugin's init:
        public static void Apply(Harmony harmony)
        {
            var type = AccessTools.TypeByName("Sandbox.Game.Gui.MyTerminalControlPanel");
            if (type == null) throw new Exception("Target type not found");

            var patchClass = typeof(MyTerminalControlPanelPatch);

            harmony.Patch(
                AccessTools.Method(type, "Init"),
                prefix: new HarmonyMethod(patchClass, nameof(InitPrefix)),
                postfix: new HarmonyMethod(patchClass, nameof(InitPostfix))
            );

            harmony.Patch(
                AccessTools.Method(type, "AddGroupToList"),
                postfix: new HarmonyMethod(patchClass, nameof(AddGroupToListPostfix))
            );

            harmony.Patch(
                AccessTools.Method(type, "TerminalSystem_GroupRemoved"),
                postfix: new HarmonyMethod(patchClass, nameof(TerminalSystem_GroupRemovedPostfix))
            );

            harmony.Patch(
                AccessTools.Method(type, "groupDelete_ButtonClicked"),
                postfix: new HarmonyMethod(patchClass, nameof(groupDelete_ButtonClickedPostfix))
            );

            harmony.Patch(
                AccessTools.Method(type, "groupSave_ButtonClicked"),
                postfix: new HarmonyMethod(patchClass, nameof(groupSave_ButtonClickedPostfix))
            );

            harmony.Patch(
                AccessTools.Method(type, "SelectBlocks", Type.EmptyTypes),
                postfix: new HarmonyMethod(patchClass, nameof(SelectBlocksPostfix))
            );

            harmony.Patch(
                AccessTools.Method(type, "TerminalSystem_BlockAdded"),
                prefix: new HarmonyMethod(patchClass, nameof(TerminalSystem_BlockAddedPrefix))
            );

            harmony.Patch(
                AccessTools.Method(type, "TerminalSystem_BlockRemoved"),
                postfix: new HarmonyMethod(patchClass, nameof(TerminalSystem_BlockRemovedPostfix))
            );

            harmony.Patch(
                AccessTools.Method(type, "Close"),
                prefix: new HarmonyMethod(patchClass, nameof(ClosePrefix))
            );

            harmony.Patch(
                AccessTools.Method(type, "blockSearch_TextChanged", new[] { typeof(string) }),
                prefix: new HarmonyMethod(patchClass, nameof(blockSearch_TextChangedPrefix))
            );

            harmony.Patch(
                AccessTools.Method(type, "PopulateBlockList"),
                transpiler: new HarmonyMethod(patchClass, nameof(PopulateBlockListTranspiler))
            );

            harmony.Patch(
                AccessTools.Method(type, "UpdateItemAppearance"),
                transpiler: new HarmonyMethod(patchClass, nameof(UpdateItemAppearanceTranspiler))
            );
        }

        private static ControlPanelLogic logic;

        [HarmonyPrefix]
        [HarmonyPatch("Init")]
        // ReSharper disable once InconsistentNaming
        private static bool InitPrefix(object __instance, IMyGuiControlsParent controlsParent)
        {
            // Config conditions are inside the constructor
            Debug.Assert(__instance != null);
            logic = new ControlPanelLogic(new MyTerminalControlPanelWrapper(__instance), controlsParent);
            return true;
        }

        [HarmonyPostfix]
        [HarmonyPatch("Init")]
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
        [HarmonyPatch("TerminalSystem_BlockAdded")]
        private static bool TerminalSystem_BlockAddedPrefix(MyTerminalBlock obj)
        {
            if (!Config.Current.EnableBlockFilter)
                return true;

            logic.BlockAdded(obj);
            return false;
        }

        [HarmonyPostfix]
        [HarmonyPatch("TerminalSystem_BlockRemoved")]
        private static void TerminalSystem_BlockRemovedPostfix(MyTerminalBlock obj)
        {
            if (!Config.Current.EnableBlockFilter)
                return;

            logic.UnregisterBlock(obj);
        }

        [HarmonyPrefix]
        [HarmonyPatch("Close")]
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
        [HarmonyPatch("blockSearch_TextChanged")]
        private static bool blockSearch_TextChangedPrefix(string text)
        {
            if (!Config.Current.EnableBlockFilter)
                return true;

            logic.blockSearch_TextChanged(text);
            return false;
        }

        [HarmonyTranspiler]
        [HarmonyPatch("PopulateBlockList")]
        private static IEnumerable<CodeInstruction> PopulateBlockListTranspiler(IEnumerable<CodeInstruction> code)
        {
            var il = code.ToList();
            il.RecordOriginalCode();

            var actual = il.Hash();
            const string expected = "60c4cb3b";
            if (actual != expected && !DisableCodeValidations)
            {
                throw new Exception("Detected code change in MyTerminalControlPanel.PopulateBlockList: actual {actual}, expected {expected}");
            }

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
        [HarmonyPatch("UpdateItemAppearance")]
        private static IEnumerable<CodeInstruction> UpdateItemAppearanceTranspiler(IEnumerable<CodeInstruction> code)
        {
            var il = code.ToList();
            il.RecordOriginalCode();

            var actual = il.Hash();
            const string expected = "378bbed9";
            if (actual != expected && !DisableCodeValidations)
            {
                throw new Exception("Detected code change in MyTerminalControlPanel.UpdateItemAppearance: actual {actual}, expected {expected}");
            }
            
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