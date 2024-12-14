using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.GameSystems;
using Sandbox.Game.Gui;
using Sandbox.Game.Screens.Helpers;
using Sandbox.Graphics.GUI;
using VRage.Utils;
using VRageMath;

[assembly: IgnoresAccessChecksTo("Sandbox.Game")]
namespace ClientPlugin.Logic
{
    public class ControlPanelLogic
    {
        private readonly MyTerminalControlPanel controlPanel;

        private bool showDefaultNames;

        private MyGuiControlCheckbox showDefaultNamesCheckbox;
        private MyGuiControlListbox blockListbox;

        private MyGuiControlCombobox modeSelectorCombobox;
        private Dictionary<int, object> modeSelectorItemData = new Dictionary<int, object>();

        private HashSet<string> blockTypes = new HashSet<string>();
        private Dictionary<long, HashSet<string>> groupsByBlock = new Dictionary<long, HashSet<string>>();
        private Dictionary<string, HashSet<long>> blocksByGroup = new Dictionary<string, HashSet<long>>();

        private object ModeSelectorData => modeSelectorItemData.GetValueOrDefault((int)modeSelectorCombobox.GetSelectedKey());

        public ControlPanelLogic(MyTerminalControlPanel controlPanel, IMyGuiControlsParent controlsParent)
        {
            Debug.Assert(controlPanel != null);
            Debug.Assert(controlsParent != null);
            
            this.controlPanel = controlPanel;

            blockListbox = (MyGuiControlListbox)controlsParent.Controls.GetControlByName("FunctionalBlockListbox");

            showDefaultNames = false;
            showDefaultNamesCheckbox = (MyGuiControlCheckbox)controlsParent.Controls.GetControlByName("ShowDefaultNames");
            showDefaultNamesCheckbox.IsChecked = showDefaultNames;
            showDefaultNamesCheckbox.Enabled = true;
            showDefaultNamesCheckbox.IsCheckedChanged += showDefaultNames_Clicked;
            showDefaultNamesCheckbox.SetToolTip(MyStringId.GetOrCompute("Show and search original block names"));

            modeSelectorCombobox = (MyGuiControlCombobox)controlsParent.Controls.GetControlByName("ModeSelector");
            modeSelectorCombobox.Enabled = true;
            modeSelectorCombobox.SelectedItemChanged += m_modeSelector_SelectedItemChanged;
            modeSelectorCombobox.SetToolTip(MyStringId.GetOrCompute("Block list mode selector"));
        }

        public void Close()
        {
            showDefaultNamesCheckbox.IsCheckedChanged -= showDefaultNames_Clicked;
            modeSelectorCombobox.SelectedItemChanged -= m_modeSelector_SelectedItemChanged;
        }

        private void m_modeSelector_SelectedItemChanged(MyGuiControlCombobox obj)
        {
            controlPanel.blockSearch_TextChanged(controlPanel.m_searchBox.SearchText);
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
            AddModeSelectorItem(BlockListMode.VisibleOnHud, "Blocks visible on HUD");

            AddBlockGroupsToModeSelector();
            AddBlockTypesToModeSelector();

            var selected = false;
            if (previousData != null)
            {
                var itemCount = modeSelectorCombobox.GetItemsCount();
                for (var i = 0; i < itemCount; i++)
                {
                    if (modeSelectorItemData[i] == previousData)
                    {
                        modeSelectorCombobox.SelectItemByIndex(i);
                        selected = true;
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

            var pattern = string.IsNullOrEmpty(text) ? null : text.ToLower().Split(' ');

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
                if (!visible)
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
            if (!showDefaultNames)
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
    }
}