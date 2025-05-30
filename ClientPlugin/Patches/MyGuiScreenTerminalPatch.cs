using System.Diagnostics.CodeAnalysis;
using ClientPlugin.Extensions;
using ClientPlugin.Logic;
using HarmonyLib;
using Sandbox.Game.Gui;
using Sandbox.Game.Screens.Helpers;
using Sandbox.Graphics.GUI;
using VRage.Game;
using VRage.Utils;
using VRageMath;

namespace ClientPlugin.Patches
{
    // ReSharper disable once UnusedType.Global
    [HarmonyPatch(typeof(MyGuiScreenTerminal))]
    [SuppressMessage("ReSharper", "UnusedMember.Local")]
    public static class MyGuiScreenTerminalPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch("CreateControlPanelPageControls")]
        // ReSharper disable once InconsistentNaming
        private static void CreateControlPanelPageControlsPostfix(MyGuiScreenTerminal __instance, MyGuiControlTabPage page)
        {
            if (Config.Current.EnableBlockFilter)
                AddModeSelector(page);

            if (Config.Current.EnableGroupRenaming)
                AddRenameGroupButton(__instance);
        }

        private static void AddModeSelector(MyGuiControlTabPage page)
        {
            var labelToHide = (MyGuiControlLabel)page.Controls.GetControlByName("ControlLabel");
            var panelToHide = page.Controls[page.Controls.IndexOf(labelToHide) - 1];

            labelToHide.Visible = false;
            panelToHide.Visible = false;

            var showAllButton = (MyGuiControlButton)page.Controls.GetControlByName("ShowAll");
            MyGuiControlCheckbox showDefaultNames = new MyGuiControlCheckbox(showAllButton.Position + new Vector2(-0.0008f, 0.046f), originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP);
            showDefaultNames.Name = "ShowDefaultNames";
            page.Controls.Add(showDefaultNames);

            var searchBox = (MyGuiControlSearchBox)page.Controls.GetControlByName("FunctionalBlockSearch");
            MyGuiControlCombobox modeSelector = new MyGuiControlCombobox(searchBox.Position + new Vector2(0f, 0.05f), searchBox.Size, originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP, isAutoscaleEnabled: true);
            modeSelector.Name = "ModeSelector";
            page.Controls.Add(modeSelector);
        }

        // ReSharper disable once InconsistentNaming
        private static void AddRenameGroupButton(MyGuiScreenTerminal __instance)
        {
            var saveGroup = __instance.GetGroupSave();
            var deleteGroup = __instance.GetGroupDelete();

            var groupButtonAreaWidth = __instance.GetGroupName().Size.X;
            var groupButtonSpacing = 0.08f * saveGroup.Size.X;
            var groupButtonSize = new Vector2((groupButtonAreaWidth - 2f * groupButtonSpacing) / 3f, saveGroup.Size.Y);
            var groupButtonStep = new Vector2(groupButtonSize.X + groupButtonSpacing, 0f);
            
            saveGroup.Size = groupButtonSize;
            
            deleteGroup.Position = saveGroup.Position + 2f * groupButtonStep;
            deleteGroup.Size = groupButtonSize;
            
            var renameGroupButton = new MyGuiControlButton(saveGroup.Position + groupButtonStep, MyGuiControlButtonStyleEnum.Rectangular, groupButtonSize, originAlign: MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_CENTER);
            renameGroupButton.Name = "GroupRename";
            renameGroupButton.Text = "Rename";
            renameGroupButton.TextEnum = MyStringId.GetOrCompute("Rename");
            renameGroupButton.ShowTooltipWhenDisabled = true;
            renameGroupButton.SetToolTip(MyStringId.GetOrCompute("Select a block group to rename it"));
            
            ControlPanelLogic.RenameGroupButton = renameGroupButton; // FIXME: Transfer of reference via global state
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(MyGuiScreenTerminal.AttachGroups))]
        // ReSharper disable once UnusedMember.Global
        public static void AttachGroupsPostfix(MyGuiControls parent)
        {
            if (!Config.Current.EnableBlockFilter)
                return;

            parent.Add(ControlPanelLogic.RenameGroupButton);
        }

        [HarmonyPostfix]
        [HarmonyPatch(nameof(MyGuiScreenTerminal.DetachGroups))]
        // ReSharper disable once UnusedMember.Global
        public static void DetachGroupsPostfix(MyGuiControls parent)
        {
            if (!Config.Current.EnableBlockFilter)
                return;

            parent.Remove(ControlPanelLogic.RenameGroupButton);
        }
    }
}