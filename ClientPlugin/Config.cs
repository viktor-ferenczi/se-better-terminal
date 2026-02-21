using ClientPlugin.Settings;
using ClientPlugin.Settings.Elements;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using ClientPlugin.Logic;


namespace ClientPlugin
{
    public class Config : INotifyPropertyChanged
    {
        #region Options

        private string defaultSearchText = "";
        private bool enableGroupRenaming = true;
        private bool enableBlockFilter = true;
        private bool enableExtendedTooltips = true;
        private bool enableContextMenu = true;
        private bool contextMenuShowInventory = true;
        private bool contextMenuShowTerminal = true;
        private bool contextMenuShowOnOff = true;
        private bool contextMenuShowHud = true;
        private bool contextMenuShowActions = true;
        private bool contextMenuShowToggles = true;
        private DoubleClickAction doubleClickAction = DoubleClickAction.None;

        #endregion

        #region User interface

        public readonly string Title = "Better Terminal";

        [Separator("Search")]

        [Textbox(description: "Enter this search text by default every time I open the Terminal")]
        public string DefaultSearchText
        {
            get => defaultSearchText;
            set => SetField(ref defaultSearchText, value);
        }

        [Separator("Control Panel")]

        [Checkbox(description: "Allow renaming groups")]
        public bool EnableGroupRenaming
        {
            get => enableGroupRenaming;
            set => SetField(ref enableGroupRenaming, value);
        }

        [Checkbox(description: "Allow block filtering and showing default names")]
        public bool EnableBlockFilter
        {
            get => enableBlockFilter;
            set => SetField(ref enableBlockFilter, value);
        }

        [Checkbox(description: "Show extended tooltips over blocks")]
        public bool EnableExtendedTooltips
        {
            get => enableExtendedTooltips;
            set => SetField(ref enableExtendedTooltips, value);
        }

        [Separator("Context Menu")]

        [Checkbox(description: "Enable right-click context menu on blocks")]
        public bool EnableContextMenu
        {
            get => enableContextMenu;
            set => SetField(ref enableContextMenu, value);
        }

        [Checkbox(description: "Context menu: Show inventory options")]
        public bool ContextMenuShowInventory
        {
            get => contextMenuShowInventory;
            set => SetField(ref contextMenuShowInventory, value);
        }

        [Checkbox(description: "Context menu: Show terminal visibility toggle")]
        public bool ContextMenuShowTerminal
        {
            get => contextMenuShowTerminal;
            set => SetField(ref contextMenuShowTerminal, value);
        }

        [Checkbox(description: "Context menu: Show ON/OFF toggle")]
        public bool ContextMenuShowOnOff
        {
            get => contextMenuShowOnOff;
            set => SetField(ref contextMenuShowOnOff, value);
        }

        [Checkbox(description: "Context menu: Show HUD visibility toggle")]
        public bool ContextMenuShowHud
        {
            get => contextMenuShowHud;
            set => SetField(ref contextMenuShowHud, value);
        }

        [Checkbox(description: "Context menu: Show block actions")]
        public bool ContextMenuShowActions
        {
            get => contextMenuShowActions;
            set => SetField(ref contextMenuShowActions, value);
        }

        [Checkbox(description: "Context menu: Show block toggles")]
        public bool ContextMenuShowToggles
        {
            get => contextMenuShowToggles;
            set => SetField(ref contextMenuShowToggles, value);
        }

        [Separator("Double Click")]

        [Dropdown(description: "Double-click action")]
        public DoubleClickAction DoubleClickAction
        {
            get => doubleClickAction;
            set => SetField(ref doubleClickAction, value);
        }

        #endregion

        #region Property change notification boilerplate

        public static readonly Config Default = new Config();
        public static readonly Config Current = ConfigStorage.Load();

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        #endregion
    }
}
