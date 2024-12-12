using System;
using System.Collections.Generic;
using System.Reflection;
using ClientPlugin.Extensions;
using HarmonyLib;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.GameSystems;
using Sandbox.Graphics.GUI;
using VRage.Utils;

namespace ClientPlugin.Logic
{
    public class ControlPanelLogic
    {
        private object controlPanel;
        private readonly IMyGuiControlsParent controlsParent;

        private readonly MethodInfo showAll_ClickedMethod;
        private readonly MethodInfo UpdateItemAppearanceMethod;
        private readonly MethodInfo TerminalSystemPropertyGetter;
        private readonly MethodInfo AddBlockToListMethod;
        private readonly FieldInfo m_originalBlockField;

        private bool m_showDefaultNames;
        private bool m_showAllTerminalBlocks;

        private MyGuiControlCheckbox m_showDefaultNamesCheckbox;
        private MyGuiControlListbox m_blockListbox;
        private MyTerminalBlock m_originalBlock;

        private MyGuiControlCombobox m_modeSelector;
        private Dictionary<int, object> m_modeSelectorData = new Dictionary<int, object>();

        private HashSet<string> m_blockTypes = new HashSet<string>();
        private Dictionary<long, HashSet<string>> m_groupsByBlock = new Dictionary<long, HashSet<string>>();
        private Dictionary<string, HashSet<long>> m_blocksByGroup = new Dictionary<string, HashSet<long>>();

        private object ModeSelectorData => m_modeSelectorData.GetValueOrDefault((int)m_modeSelector.GetSelectedKey());

        public ControlPanelLogic(object controlPanel, IMyGuiControlsParent controlsParent)
        {
            this.controlPanel = controlPanel;
            this.controlsParent = controlsParent;

            var controlPanelType = controlPanel.GetType();
            
            showAll_ClickedMethod = AccessTools.DeclaredMethod(controlPanelType, "showAll_Clicked");
            UpdateItemAppearanceMethod = AccessTools.DeclaredMethod(controlPanelType, "UpdateItemAppearance");
            AddBlockToListMethod = AccessTools.DeclaredMethod(controlPanelType, "AddBlockToList");

            TerminalSystemPropertyGetter = AccessTools.DeclaredPropertyGetter(controlPanelType, "TerminalSystem");
            m_originalBlockField = AccessTools.DeclaredField(controlPanelType, "m_originalBlock");
        }

        public void Init()
        {
            m_blockListbox = (MyGuiControlListbox)controlsParent.Controls.GetControlByName("FunctionalBlockListbox");
            m_originalBlock = (MyTerminalBlock)m_originalBlockField.GetValue(controlPanel);

            m_showDefaultNames = false;
            m_showDefaultNamesCheckbox = (MyGuiControlCheckbox)controlsParent.Controls.GetControlByName("ShowDefaultNames");
            m_showDefaultNamesCheckbox.IsChecked = m_showDefaultNames;
            m_showDefaultNamesCheckbox.Enabled = true;
            m_showDefaultNamesCheckbox.IsCheckedChanged += showDefaultNames_Clicked;
            m_showDefaultNamesCheckbox.SetToolTip(MyStringId.GetOrCompute("Show and search original block names"));

            m_modeSelector = (MyGuiControlCombobox)controlsParent.Controls.GetControlByName("ModeSelector");
            m_modeSelector.Enabled = true;
            m_modeSelector.SelectedItemChanged += m_modeSelector_SelectedItemChanged;
            m_modeSelector.SetToolTip(MyStringId.GetOrCompute("Block list mode selector"));
        }

        private void m_modeSelector_SelectedItemChanged(MyGuiControlCombobox obj)
        {
            m_showAllTerminalBlocks = !m_showAllTerminalBlocks;
            // controlPanel.showAll_Clicked(null);
            showAll_ClickedMethod.Invoke(controlPanel, new object[] { null });
        }

        private void showDefaultNames_Clicked(MyGuiControlCheckbox obj)
        {
            m_showDefaultNames = !m_showDefaultNames;

            var items = m_blockListbox.Items;
            foreach (var item in items)
            {
                if (item.UserData is MyTerminalBlock block)
                {
                    //controlPanel.UpdateItemAppearance(block, item);
                    UpdateItemAppearanceMethod.Invoke(controlPanel, new object[] { block, item });
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

            m_modeSelector.ClearItems();
            m_modeSelectorData.Clear();

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
                var itemCount = m_modeSelector.GetItemsCount();
                for (var i = 0; i < itemCount; i++)
                {
                    if (m_modeSelectorData[i] == previousData)
                    {
                        m_modeSelector.SelectItemByIndex(i);
                        selected = true;
                        break;
                    }
                }
            }

            if (!selected)
            {
                m_modeSelector.SelectItemByIndex(0);
            }
        }

        private void AddBlockGroupsToModeSelector()
        {
            var terminalSystem = (MyGridTerminalSystem)TerminalSystemPropertyGetter.Invoke(controlPanel, new object[] { });

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
            var blockTypeList = new List<string>(m_blockTypes);
            blockTypeList.Sort();

            AddModeSelectorItem(null, "======== BLOCK TYPES ==========");
            foreach (var blockTypeName in blockTypeList)
            {
                AddModeSelectorItem(blockTypeName, blockTypeName);
            }
        }

        private void AddModeSelectorItem(object data, string label)
        {
            var key = m_modeSelector.GetItemsCount();
            m_modeSelectorData[key] = data;
            m_modeSelector.AddItem(key, MyStringId.GetOrCompute(label), key);
        }

        public void blockSearch_TextChanged(string text)
        {
            if (m_blockListbox == null)
                return;

            var pattern = string.IsNullOrEmpty(text) ? null : text.ToLower().Split(' ');

            var defaultMode = false;
            var showHiddenBlocks = m_showAllTerminalBlocks;

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

            var groupCount = m_blocksByGroup.Count;
            var visibleGroupNames = new HashSet<string>(groupCount);

            foreach (var item in m_blockListbox.Items)
            {
                if (!(item.UserData is MyTerminalBlock terminalBlock))
                    continue;

                var visible = (terminalBlock.ShowInTerminal || showHiddenBlocks || (defaultMode && terminalBlock == m_originalBlock)) &&
                              IsBlockShownInMode(terminalBlock, modeSelectorData) && IsMatchingItem(item, pattern);

                item.Visible = visible;
                if (!visible)
                    continue;

                if (m_groupsByBlock.TryGetValue(terminalBlock.EntityId, out var groupNames))
                {
                    foreach (var groupName in groupNames)
                    {
                        visibleGroupNames.Add(groupName);
                    }
                }
            }

            foreach (var item in m_blockListbox.Items)
            {
                if (!(item.UserData is MyBlockGroup blockGroup))
                    continue;

                item.Visible = visibleGroupNames.Contains(blockGroup.Name.ToString()) && IsMatchingItem(item, pattern);
            }

            m_blockListbox.ScrollToolbarToTop();
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
                            return terminalBlock.CubeGrid.IsSameConstructAs(m_originalBlock.CubeGrid);

                        case BlockListMode.Subgrid:
                            return terminalBlock.CubeGrid.EntityId == m_originalBlock.CubeGrid.EntityId;

                        case BlockListMode.Ungrouped:
                            return !m_groupsByBlock.TryGetValue(terminalBlock.EntityId, out var groups1) || groups1.Count == 0;

                        case BlockListMode.Multigrouped:
                            return m_groupsByBlock.TryGetValue(terminalBlock.EntityId, out var groups2) && groups2.Count > 1;

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
                    return m_groupsByBlock.TryGetValue(terminalBlock.EntityId, out var groupNames) && groupNames.Contains(blockGroup.Name.ToString());

                case string blockTypeName:
                    return terminalBlock.BlockDefinition.DisplayNameText == blockTypeName;
            }

            return false;
        }

        public void RegisterBlock(MyTerminalBlock terminalBlock)
        {
            m_blockTypes.Add(terminalBlock.BlockDefinition.DisplayNameText);
        }

        public void UnregisterBlock(MyTerminalBlock terminalBlock)
        {
            var entityId = terminalBlock.EntityId;
            m_groupsByBlock.Remove(entityId);
            foreach (var blockIds in m_blocksByGroup.Values)
            {
                blockIds.Remove(entityId);
            }
        }

        public void RegisterGroup(MyBlockGroup group)
        {
            var groupName = group.Name.ToString();

            if (!m_blocksByGroup.TryGetValue(groupName, out var blocks))
            {
                m_blocksByGroup[groupName] = blocks = new HashSet<long>();
            }

            foreach (var terminalBlock in group.GetBlocksField())
            {
                var entityId = terminalBlock.EntityId;
                blocks.Add(entityId);
                if (!m_groupsByBlock.TryGetValue(entityId, out var groups))
                {
                    m_groupsByBlock[entityId] = groups = new HashSet<string>();
                }

                groups.Add(groupName);
            }
        }

        public void UnregisterGroup(MyBlockGroup group)
        {
            var groupName = group.Name.ToString();

            m_blocksByGroup.Remove(groupName);

            foreach (var groupNames in m_groupsByBlock.Values)
            {
                groupNames.Remove(groupName);
            }
        }

        public void BlockAdded(MyTerminalBlock myTerminalBlock)
        {
            RegisterBlock(myTerminalBlock);
            var visible = (myTerminalBlock == m_originalBlock || myTerminalBlock.ShowInTerminal || m_showAllTerminalBlocks) && IsBlockShownInMode(myTerminalBlock, ModeSelectorData);
            AddBlockToListMethod.Invoke(controlPanel, new object[] { myTerminalBlock, visible });
        }
    }
}