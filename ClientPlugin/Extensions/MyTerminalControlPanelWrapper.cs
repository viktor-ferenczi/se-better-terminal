using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.GameSystems;
using Sandbox.Game.Screens.Helpers;
using Sandbox.Graphics.GUI;
// ReSharper disable InconsistentNaming

namespace ClientPlugin.Extensions
{
    public readonly struct MyTerminalControlPanelWrapper
    {
        private static readonly Type Type = AccessTools.TypeByName("Sandbox.Game.Gui.MyTerminalControlPanel");

        private static readonly FieldInfo GroupNameField = AccessTools.DeclaredField(Type, "m_groupName");
        private static readonly FieldInfo SearchBoxField = AccessTools.DeclaredField(Type, "m_searchBox");
        private static readonly FieldInfo ShowAllButtonField = AccessTools.DeclaredField(Type, "m_showAll");
        private static readonly FieldInfo ShowAllTerminalBlocksField = AccessTools.DeclaredField(Type, "m_showAllTerminalBlocks");
        private static readonly FieldInfo OriginalBlockField = AccessTools.DeclaredField(Type, "m_originalBlock");
        private static readonly FieldInfo GroupDeleteButtonField = AccessTools.DeclaredField(Type, "m_groupDelete");
        private static readonly FieldInfo CurrentGroupsField = AccessTools.DeclaredField(Type, "m_currentGroups");

        private static readonly PropertyInfo TerminalSystemProperty = AccessTools.DeclaredProperty(Type, "TerminalSystem");
        
        private static readonly MethodInfo UpdateItemAppearanceMethod = AccessTools.DeclaredMethod(Type, "UpdateItemAppearance", new[] { typeof(MyTerminalBlock), typeof(MyGuiControlListbox.Item) });
        private static readonly MethodInfo AddBlockToListMethod = AccessTools.DeclaredMethod(Type, "AddBlockToList", new[] { typeof(MyTerminalBlock), typeof(bool?) });
        private static readonly MethodInfo GroupSaveButtonClickedMethod = AccessTools.DeclaredMethod(Type, "groupSave_ButtonClicked", new[] { typeof(MyGuiControlButton) });
        private static readonly MethodInfo BlockListboxItemSelectedMethod = AccessTools.DeclaredMethod(Type, "blockListbox_ItemSelected", new[] { typeof(MyGuiControlListbox) });
        private static readonly MethodInfo BlockSearchTextChangedMethod = AccessTools.DeclaredMethod(Type, "blockSearch_TextChanged", new[] { typeof(string) });
        private static readonly MethodInfo RefreshBlockListMethod = AccessTools.DeclaredMethod(Type, "RefreshBlockList", new[] { typeof(MyTerminalBlock[]) });

        private readonly object instance;

        public MyTerminalControlPanelWrapper(object instance)
        {
            this.instance = instance;
        }

        public MyGuiControlTextbox m_groupName => (MyGuiControlTextbox)GroupNameField.GetValue(instance);
        public MyGuiControlSearchBox m_searchBox => (MyGuiControlSearchBox)SearchBoxField.GetValue(instance);
        public MyGuiControlButton m_showAll => (MyGuiControlButton)ShowAllButtonField.GetValue(instance);

        public static bool ShowAllTerminalBlocks
        {
            get => (bool)ShowAllTerminalBlocksField.GetValue(null);
            set => ShowAllTerminalBlocksField.SetValue(null, value);
        }

        public MyTerminalBlock m_originalBlock => (MyTerminalBlock)OriginalBlockField.GetValue(instance);
        public MyGuiControlButton m_groupDelete => (MyGuiControlButton)GroupDeleteButtonField.GetValue(instance);
        public List<MyBlockGroup> m_currentGroups => (List<MyBlockGroup>)CurrentGroupsField.GetValue(instance);

        public MyGridTerminalSystem TerminalSystem => (MyGridTerminalSystem)TerminalSystemProperty.GetValue(instance);
        
        public void UpdateItemAppearance(MyTerminalBlock block, MyGuiControlListbox.Item item)
        {
            UpdateItemAppearanceMethod.Invoke(instance, new object[] { block, item });
        }

        public MyGuiControlListbox.Item AddBlockToList(MyTerminalBlock block, bool? visibility = null)
        {
            return (MyGuiControlListbox.Item)AddBlockToListMethod.Invoke(instance, new object[] { block, visibility });
        }

        public void groupSave_ButtonClicked(MyGuiControlButton button)
        {
            GroupSaveButtonClickedMethod.Invoke(instance, new object[] { button });
        }

        public void blockListbox_ItemSelected(MyGuiControlListbox listbox)
        {
            BlockListboxItemSelectedMethod.Invoke(instance, new object[] { listbox });
        }

        public void blockSearch_TextChanged(string searchText)
        {
            BlockSearchTextChangedMethod.Invoke(instance, new object[] { searchText });
        }
        
        public void RefreshBlockList(MyTerminalBlock[] selectedBlocks = null)
        {
            RefreshBlockListMethod.Invoke(instance, new object[] { selectedBlocks });
        }
    }
}