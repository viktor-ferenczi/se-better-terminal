using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.GameSystems;
using Sandbox.Game.Gui;
using Sandbox.Game.Screens.Helpers;
using Sandbox.Graphics.GUI;
using VRage.Utils;
using VRageMath;
using ClientPlugin.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Sandbox.Common.ObjectBuilders;

namespace ClientPlugin.Logic
{
    public class ControlPanelLogic
    {
        // FIXME: Transfer of reference via global state
        public static MyGuiControlButton RenameGroupButton;

        private readonly MyTerminalControlPanel controlPanel;

        private bool showDefaultNames;
        
        private bool groupRenamingInitialized;
        private string originalGroupName;

        private bool blockFilterInitialized;
        private MyGuiControlCheckbox showDefaultNamesCheckbox;
        private MyGuiControlListbox blockListbox;

        private MyGuiControlCombobox modeSelectorCombobox;
        private Dictionary<int, object> modeSelectorItemData = new Dictionary<int, object>();

        private HashSet<string> blockTypes = new HashSet<string>();
        private Dictionary<long, HashSet<string>> groupsByBlock = new Dictionary<long, HashSet<string>>();
        private Dictionary<string, HashSet<long>> blocksByGroup = new Dictionary<string, HashSet<long>>();

        private object ModeSelectorData => modeSelectorItemData.GetValueOrDefault((int)modeSelectorCombobox.GetSelectedKey(), BlockListMode.Default);
        public bool IsModeSelectorEmpty => modeSelectorItemData.Count == 0;

        public ControlPanelLogic(MyTerminalControlPanel controlPanel, IMyGuiControlsParent controlsParent)
        {
            Debug.Assert(controlPanel != null);
            Debug.Assert(controlsParent != null);

            this.controlPanel = controlPanel;

            PrepareModeSelector(controlsParent);
        }

        private void PrepareModeSelector(IMyGuiControlsParent controlsParent)
        {
            if (blockFilterInitialized || !Config.Current.EnableBlockFilter)
                return;
            
            blockListbox = (MyGuiControlListbox)controlsParent.Controls.GetControlByName("FunctionalBlockListbox");

            showDefaultNames = false;

            showDefaultNamesCheckbox = (MyGuiControlCheckbox)controlsParent.Controls.GetControlByName("ShowDefaultNames");
            showDefaultNamesCheckbox.IsChecked = showDefaultNames;
            showDefaultNamesCheckbox.Enabled = true;
            showDefaultNamesCheckbox.IsCheckedChanged += showDefaultNames_Clicked;
            showDefaultNamesCheckbox.SetToolTip(MyStringId.GetOrCompute("Show and search original block names"));

            modeSelectorCombobox = (MyGuiControlCombobox)controlsParent.Controls.GetControlByName("ModeSelector");
            modeSelectorCombobox.Enabled = true;
            modeSelectorCombobox.SelectedItemChanged += OnModeChanged;
            modeSelectorCombobox.SetToolTip(MyStringId.GetOrCompute("Block list mode selector"));

            blockFilterInitialized = true;
        }

        public void PrepareGroupRenaming()
        {
            if (groupRenamingInitialized || !Config.Current.EnableGroupRenaming || RenameGroupButton == null || controlPanel.m_groupName == null)
                return;

            originalGroupName = "";
            
            RenameGroupButton.Enabled = false;
            RenameGroupButton.SetToolTip(MyStringId.GetOrCompute("Rename group"));
            RenameGroupButton.ButtonClicked += OnRenameGroupButtonClicked;

            controlPanel.m_groupName.TextChanged += OnGroupNameChanged;

            groupRenamingInitialized = true;
        }

        private void SetOriginalGroupName(string groupName)
        {
            originalGroupName = groupName;

            if (groupRenamingInitialized)
            {
                RenameGroupButton.Enabled = false;
                RenameGroupButton.SetToolTip(groupName == ""
                    ? MyStringId.GetOrCompute("Select a block group to rename it")
                    : MyStringId.GetOrCompute($"Rename group: {groupName}"));
            }
        }

        public void Close()
        {
            if (blockFilterInitialized)
            {
                showDefaultNamesCheckbox.IsCheckedChanged -= showDefaultNames_Clicked;
                modeSelectorCombobox.SelectedItemChanged -= OnModeChanged;
                modeSelectorItemData.Clear();
                groupsByBlock.Clear();
                blocksByGroup.Clear();
                blockFilterInitialized = false;
            }

            if (groupRenamingInitialized)
            {
                RenameGroupButton.ButtonClicked -= OnRenameGroupButtonClicked;
                controlPanel.m_groupName.TextChanged -= OnGroupNameChanged;
                RenameGroupButton = null;
                groupRenamingInitialized = false;
            }
        }

        private void OnModeChanged(MyGuiControlCombobox obj)
        {
            controlPanel.blockSearch_TextChanged(controlPanel.m_searchBox.SearchText);
            EnableShowAllOnlyInRelevantModes();
        }

        private void EnableShowAllOnlyInRelevantModes()
        {
            var enableShowAll = false;
            if (ModeSelectorData is BlockListMode mode)
            {
                switch (mode)
                {
                    case BlockListMode.Default:
                    case BlockListMode.ShipOrStation:
                    case BlockListMode.Subgrid:
                        enableShowAll = true; 
                        break;
                }
            }
            controlPanel.m_showAll.Visible = enableShowAll;
            controlPanel.m_showAll.Enabled = enableShowAll;
        }

        private void showDefaultNames_Clicked(MyGuiControlCheckbox obj)
        {
            showDefaultNames = !showDefaultNames;

            var items = blockListbox.Items;
            foreach (var item in items)
            {
                if (item.UserData is MyTerminalBlock block)
                {
                    controlPanel.UpdateItemAppearance(block, item);
                }
            }
        }

        private enum BlockListMode
        {
            Default,
            ShipOrStation,
            Subgrid,
            Ungrouped,
            Multigrouped,
            Damaged,
            Disabled,
            Enabled,
            VisibleOnHud,
        }

        public void UpdateModeSelector()
        {
            var previousData = ModeSelectorData;

            modeSelectorCombobox.ClearItems();
            modeSelectorItemData.Clear();

            AddModeSelectorItem(BlockListMode.Default, "Anywhere (default mode)");
            AddModeSelectorItem(BlockListMode.ShipOrStation, "This ship or station only");
            AddModeSelectorItem(BlockListMode.Subgrid, "This subgrid only");
            AddModeSelectorItem(BlockListMode.Ungrouped, "Blocks not in any group");
            AddModeSelectorItem(BlockListMode.Multigrouped, "Blocks in multiple groups");
            AddModeSelectorItem(BlockListMode.Damaged, "Damaged terminal blocks");
            AddModeSelectorItem(BlockListMode.Disabled, "Disabled functional blocks");
            AddModeSelectorItem(BlockListMode.Enabled, "Enabled functional blocks");
            AddModeSelectorItem(BlockListMode.VisibleOnHud, "Blocks shown on HUD");

            AddBlockGroupsToModeSelector();
            AddBlockTypesToModeSelector();

            var selected = false;
            if (previousData != null)
            {
                var itemCount = modeSelectorCombobox.GetItemsCount();
                for (var i = 0; i < itemCount; i++)
                {
                    var key = modeSelectorItemData[i];
                    switch (key)
                    {
                        case BlockListMode mode:
                            selected = previousData is BlockListMode previousMode && mode == previousMode;
                            break;
                        
                        case MyBlockGroup group:
                            selected = previousData is MyBlockGroup previousGroup && group.Name == previousGroup.Name;
                            break;
                            
                        case string blockType:
                            selected = previousData is string previousBlockType && blockType == previousBlockType;
                            break;
                    }
                    if (selected)
                    {
                        modeSelectorCombobox.SelectItemByIndex(i);
                        break;
                    }
                }
            }

            if (!selected)
            {
                modeSelectorCombobox.SelectItemByIndex(0);
            }
        }

        private void AddBlockGroupsToModeSelector()
        {
            var terminalSystem = controlPanel.TerminalSystem;
            if (terminalSystem == null)
                return;

            if (terminalSystem.BlockGroups == null)
                return;

            var blockGroups = terminalSystem.BlockGroups.ToArray();
            if (blockGroups.Length == 0)
                return;

            Array.Sort(blockGroups, MyTerminalComparer.Static);

            AddModeSelectorItem(null, "======== NAMED GROUPS ========");
            foreach (var blockGroup in blockGroups)
            {
                AddModeSelectorItem(blockGroup, blockGroup.Name.ToString());
            }
        }

        private void AddBlockTypesToModeSelector()
        {
            var blockTypeList = new List<string>(blockTypes);
            blockTypeList.Sort();

            AddModeSelectorItem(null, "======== BLOCK TYPES ==========");
            foreach (var blockTypeName in blockTypeList)
            {
                AddModeSelectorItem(blockTypeName, blockTypeName);
            }
        }

        private void AddModeSelectorItem(object data, string label)
        {
            var key = modeSelectorCombobox.GetItemsCount();
            modeSelectorItemData[key] = data;
            modeSelectorCombobox.AddItem(key, MyStringId.GetOrCompute(label), key);
        }

        public void blockSearch_TextChanged(string text)
        {
            if (blockListbox == null)
                return;
            
            // This is required when "Show all block" is toggled
            if (modeSelectorItemData.Count == 0)
                UpdateModeSelector();

            var pattern = string.IsNullOrEmpty(text) ? null : text.ToLower().Split(' ').Where(part => !string.IsNullOrEmpty(part)).ToArray();
            if (pattern != null && pattern.Length == 0)
            {
                pattern = null;
            }

            var defaultMode = false;
            var showHiddenBlocks = MyTerminalControlPanel.m_showAllTerminalBlocks;

            var modeSelectorData = ModeSelectorData;
            if (modeSelectorData is BlockListMode mode)
            {
                switch (mode)
                {
                    case BlockListMode.Default:
                        defaultMode = true;
                        break;

                    case BlockListMode.Ungrouped:
                    case BlockListMode.Multigrouped:
                    case BlockListMode.Damaged:
                    case BlockListMode.Disabled:
                    case BlockListMode.Enabled:
                    case BlockListMode.VisibleOnHud:
                        showHiddenBlocks = true;
                        break;
                }
            }
            else
            {
                showHiddenBlocks = true;
            }

            var originalBlock = controlPanel.m_originalBlock;

            var groupCount = blocksByGroup.Count;
            var visibleGroupNames = new HashSet<string>(groupCount);

            foreach (var item in blockListbox.Items)
            {
                if (!(item.UserData is MyTerminalBlock terminalBlock))
                    continue;

                var visible = (terminalBlock.ShowInTerminal || showHiddenBlocks || (defaultMode && terminalBlock == originalBlock)) &&
                              IsBlockShownInMode(terminalBlock, modeSelectorData) && IsMatchingItem(item, pattern);

                item.Visible = visible;
                if (!visible && !defaultMode)
                    continue;

                if (groupsByBlock.TryGetValue(terminalBlock.EntityId, out var groupNames))
                {
                    foreach (var groupName in groupNames)
                    {
                        visibleGroupNames.Add(groupName);
                    }
                }
            }

            foreach (var item in blockListbox.Items)
            {
                if (!(item.UserData is MyBlockGroup blockGroup))
                    continue;

                item.Visible = visibleGroupNames.Contains(blockGroup.Name.ToString()) && IsMatchingItem(item, pattern);
            }

            var firstSelectedVisibleItem = blockListbox.SelectedItems.FirstOrDefault(item => item.Visible);
            var firstSelectedVisibleItemPosition = -1;
            var visibleItemPosition = 0;
            foreach (var item in blockListbox.Items)
            {
                if (!item.Visible)
                    continue;

                if (item == firstSelectedVisibleItem)
                {
                    firstSelectedVisibleItemPosition = visibleItemPosition;
                    break;
                }

                visibleItemPosition++;
            }

            if (firstSelectedVisibleItemPosition >= 0)
                blockListbox.SetScrollPosition(firstSelectedVisibleItemPosition);
            else
                blockListbox.ScrollToolbarToTop();
        }

        private bool IsMatchingItem(MyGuiControlListbox.Item item, string[] pattern)
        {
            if (pattern == null)
                return true;

            var tooltip = item.ToolTip.ToolTips.Count == 0 ? "" : item.ToolTip.ToolTips[0].Text.ToString();
            var text = $@"{item.Text} {tooltip}".ToLower();

            foreach (var part in pattern)
            {
                if (!text.Contains(part))
                    return false;
            }

            return true;
        }

        private bool IsBlockShownInMode(MyTerminalBlock terminalBlock, object modeSelectorData)
        {
            switch (modeSelectorData)
            {
                case BlockListMode mode:
                    switch (mode)
                    {
                        case BlockListMode.Default:
                            break;

                        case BlockListMode.ShipOrStation:
                            return terminalBlock.CubeGrid.IsSameConstructAs(controlPanel.m_originalBlock.CubeGrid);

                        case BlockListMode.Subgrid:
                            return terminalBlock.CubeGrid.EntityId == controlPanel.m_originalBlock.CubeGrid.EntityId;

                        case BlockListMode.Ungrouped:
                            return !groupsByBlock.TryGetValue(terminalBlock.EntityId, out var groups1) || groups1.Count == 0;

                        case BlockListMode.Multigrouped:
                            return groupsByBlock.TryGetValue(terminalBlock.EntityId, out var groups2) && groups2.Count > 1;

                        case BlockListMode.Damaged:
                            return terminalBlock.SlimBlock.Integrity < terminalBlock.SlimBlock.MaxIntegrity;

                        case BlockListMode.Disabled:
                            return terminalBlock is MyFunctionalBlock functionalBlock1 && !functionalBlock1.Enabled;

                        case BlockListMode.Enabled:
                            return terminalBlock is MyFunctionalBlock functionalBlock2 && functionalBlock2.Enabled;

                        case BlockListMode.VisibleOnHud:
                            return terminalBlock.ShowOnHUD;
                    }

                    return true;

                case MyBlockGroup blockGroup:
                    return groupsByBlock.TryGetValue(terminalBlock.EntityId, out var groupNames) && groupNames.Contains(blockGroup.Name.ToString());

                case string blockTypeName:
                    return terminalBlock.BlockDefinition.DisplayNameText == blockTypeName;
            }

            return false;
        }

        public void RegisterBlock(MyTerminalBlock terminalBlock)
        {
            blockTypes.Add(terminalBlock.BlockDefinition.DisplayNameText);
        }

        public void UnregisterBlock(MyTerminalBlock terminalBlock)
        {
            var entityId = terminalBlock.EntityId;
            groupsByBlock.Remove(entityId);
            foreach (var blockIds in blocksByGroup.Values)
            {
                blockIds.Remove(entityId);
            }
        }

        public void RegisterGroup(MyBlockGroup group)
        {
            var groupName = group.Name.ToString();

            if (!blocksByGroup.TryGetValue(groupName, out var blocks))
            {
                blocksByGroup[groupName] = blocks = new HashSet<long>();
            }

            foreach (var terminalBlock in group.Blocks)
            {
                var entityId = terminalBlock.EntityId;
                blocks.Add(entityId);
                if (!groupsByBlock.TryGetValue(entityId, out var groups))
                {
                    groupsByBlock[entityId] = groups = new HashSet<string>();
                }

                groups.Add(groupName);
            }
        }

        public void UnregisterGroup(MyBlockGroup group)
        {
            var groupName = group.Name.ToString();

            blocksByGroup.Remove(groupName);

            foreach (var groupNames in groupsByBlock.Values)
            {
                groupNames.Remove(groupName);
            }
        }

        public void BlockAdded(MyTerminalBlock myTerminalBlock)
        {
            RegisterBlock(myTerminalBlock);
            var visible = (myTerminalBlock == controlPanel.m_originalBlock || myTerminalBlock.ShowInTerminal || MyTerminalControlPanel.m_showAllTerminalBlocks) && IsBlockShownInMode(myTerminalBlock, ModeSelectorData);
            controlPanel.AddBlockToList(myTerminalBlock, visible);
        }

        public void PopulateBlockList_AddBlocks(MyTerminalBlock[] blocks)
        {
            if (!Config.Current.EnableBlockFilter)
            {
                foreach (var terminalBlock in blocks)
                {
                    controlPanel.AddBlockToList(terminalBlock);
                }

                return;
            }

            var modeSelectorData = ModeSelectorData;
            var originalBlock = controlPanel.m_originalBlock;
            var showAllTerminalBlocks = MyTerminalControlPanel.m_showAllTerminalBlocks;
            foreach (var terminalBlock in blocks)
            {
                RegisterBlock(terminalBlock);
                var visible = (terminalBlock == originalBlock || terminalBlock.ShowInTerminal || showAllTerminalBlocks) && IsBlockShownInMode(terminalBlock, modeSelectorData);
                controlPanel.AddBlockToList(terminalBlock, visible);
            }
        }

        public void UpdateItemAppearance_DefaultNameImplementation(MyTerminalBlock block, MyGuiControlListbox.Item item)
        {
            var itemText = item.Text;
            if (!Config.Current.EnableBlockFilter || !showDefaultNames)
            {
                block.GetTerminalName(itemText);
                return;
            }

            itemText.Append(block.m_defaultCustomName.ToString().TrimEnd());

            if (block is MyThrust thruster && thruster.GridThrustDirection != Vector3I.Zero)
            {
                itemText.Append($" ({thruster.GetDirectionString()})");
            }
        }

        private void OnGroupNameChanged(MyGuiControlTextbox groupNameTextbox)
        {
            if (!groupRenamingInitialized)
                return;
            
            var newName = groupNameTextbox.Text.Trim();
            RenameGroupButton.Enabled = controlPanel.m_groupDelete.Enabled &&
                                        originalGroupName != "" &&
                                        newName != originalGroupName &&
                                        newName.Trim() != "";
        }

        public void AfterSelectBlocks()
        {
            PrepareGroupRenaming();
            
            var currentGroups = controlPanel.m_currentGroups;
            SetOriginalGroupName(currentGroups.Count == 1 ? currentGroups[0].Name.ToString().Trim() : "");
        }

        private void OnRenameGroupButtonClicked(MyGuiControlButton obj)
        {
            var currentGroups = controlPanel.m_currentGroups;
            if (currentGroups.Count != 1)
            {
                MyLog.Default.Warning($"BetterTerminal: No loaded group to rename");
                return;
            }

            var oldGroup = currentGroups[0];
            var oldName = oldGroup.Name.ToString();
            var newName = controlPanel.m_groupName.Text.Trim();

#if DEBUG
            MyLog.Default.Info($"BetterTerminal: Renaming group: {oldName} => {newName}");
#endif

            // Make sure no group with that new name exists
            var groupKeyInModeSelector = -1;
            foreach (var (key, data) in modeSelectorItemData)
            {
                if (!(data is MyBlockGroup blockGroup))
                    continue;

                var existingGroupName = blockGroup.Name.ToString();
                if (existingGroupName == newName)
                {
                    MyGuiSandbox.AddScreen(
                        MyGuiSandbox.CreateMessageBox(
                            messageText: new StringBuilder($"Group already exists: {newName}"),
                            messageCaption: new StringBuilder("Better Terminal: Error")));
                    return;
                }

                if (existingGroupName == oldName)
                {
                    groupKeyInModeSelector = key;
                }
            }

            if (groupKeyInModeSelector < 0)
            {
                MyLog.Default.Warning($"BetterTerminal: Cannot find existing group to rename: {oldName}");
                return;
            }

#if DEBUG
            MyLog.Default.Info($"BetterTerminal: Good, there is no colliding group");
#endif

            // Create a group with the new name, so it will contain the same blocks as the old group
#if DEBUG
            MyLog.Default.Info($"BetterTerminal: Saving as new group: {oldName}");
#endif
            controlPanel.groupSave_ButtonClicked(null);
            if (controlPanel.m_currentGroups.Count != 1 || controlPanel.m_currentGroups[0].Name.ToString() != newName)
            {
                MyLog.Default.Warning($"BetterTerminal: Failed to create new group: {newName}");
                return;
            }

            // Redirect references in block toolbar slots to the new group
            foreach (var blockListitem in blockListbox.Items)
            {
                if (!(blockListitem.UserData is MyTerminalBlock terminalBlock))
                    continue;

                var toolbar = terminalBlock.GetToolbar();
                if (toolbar == null)
                    continue;

                var toolbarBuilder = toolbar.GetObjectBuilder();
                foreach (var slotBuilder in toolbarBuilder.Slots)
                {
                    if (!(slotBuilder.Data is MyObjectBuilder_ToolbarItemTerminalGroup toolbarItemTerminalGroupBuilder))
                        continue;

                    if (toolbarItemTerminalGroupBuilder.GroupName != oldName)
                        continue;

#if DEBUG
                    MyLog.Default.Info($"BetterTerminal: Redirecting group in toolbar slot {slotBuilder.Index} of {terminalBlock.GetSafeName()}");
#endif
                    toolbarItemTerminalGroupBuilder.GroupName = newName;
                    toolbar.SetItemAtSlot(slotBuilder.Index, MyToolbarItemFactory.CreateToolbarItem(toolbarItemTerminalGroupBuilder));
                }
            }

            // Delete the old group
#if DEBUG
            MyLog.Default.Info($"BetterTerminal: Deleting old group: {oldName}");
#endif
            controlPanel.TerminalSystem.RemoveGroup(oldGroup, true);

#if DEBUG
            MyLog.Default.Info($"BetterTerminal: Finished renaming group: {oldName} => {newName}");
#endif

            controlPanel.RefreshBlockList();
            blockSearch_TextChanged(controlPanel.m_searchBox.SearchText);
            blockListbox.ScrollToolbarToTop();

            foreach (var item in blockListbox.Items)
            {
                if (item.UserData is MyBlockGroup group && group.Name.ToString() == newName)
                {
                    blockListbox.SelectSingleItem(item);
                    controlPanel.blockListbox_ItemSelected(blockListbox);
                    break;
                }
            }
        }

        private MyTerminalBlock[] GetSelectedBlocks()
        {
            return blockListbox.SelectedItems
                .Where(item => item.UserData is MyTerminalBlock)
                .Select(item => (MyTerminalBlock)item.UserData)
                .ToArray();
        }

        public void ScrollBlockListToTop()
        {
            blockListbox.ScrollToolbarToTop();
        }

        public void SetSearchText(string searchText)
        {
            if (controlPanel.m_searchBox == null)
                return;
            
            if (searchText == null || controlPanel.m_searchBox.SearchText == searchText)
                return;

            controlPanel.m_searchBox.SearchText = searchText;
            controlPanel.blockSearch_TextChanged(searchText);
        }
    }
}