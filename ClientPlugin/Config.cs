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

        public readonly string Title = "Better Terminal";

        private string defaultSearchText = "";
        [Textbox(description: "Enter this search text by default every time I open the Terminal")]
        public string DefaultSearchText
        {
            get => defaultSearchText;
            set => SetField(ref defaultSearchText, value);
        }

        private bool enableGroupRenaming = true;
        [Checkbox(description: "Allow renaming groups")]
        public bool EnableGroupRenaming
        {
            get => enableGroupRenaming;
            set => SetField(ref enableGroupRenaming, value);
        }

        private bool enableBlockFilter = true;
        [Checkbox(description: "Allow block filtering and showing default names")]
        public bool EnableBlockFilter
        {
            get => enableBlockFilter;
            set => SetField(ref enableBlockFilter, value);
        }

        private bool enableExtendedTooltips = true;
        [Checkbox(description: "Show extended tooltips over blocks")]
        public bool EnableExtendedTooltips
        {
            get => enableExtendedTooltips;
            set => SetField(ref enableExtendedTooltips, value);
        }

        private bool enableContextMenu = true;
        [Checkbox(description: "Enable right-click context menu on blocks")]
        public bool EnableContextMenu
        {
            get => enableContextMenu;
            set => SetField(ref enableContextMenu, value);
        }

        private bool contextMenuShowInventory = true;
        [Checkbox(description: "Context menu: Show inventory options")]
        public bool ContextMenuShowInventory
        {
            get => contextMenuShowInventory;
            set => SetField(ref contextMenuShowInventory, value);
        }

        private bool contextMenuShowTerminal = true;
        [Checkbox(description: "Context menu: Show terminal visibility toggle")]
        public bool ContextMenuShowTerminal
        {
            get => contextMenuShowTerminal;
            set => SetField(ref contextMenuShowTerminal, value);
        }

        private bool contextMenuShowOnOff = true;
        [Checkbox(description: "Context menu: Show ON/OFF toggle")]
        public bool ContextMenuShowOnOff
        {
            get => contextMenuShowOnOff;
            set => SetField(ref contextMenuShowOnOff, value);
        }

        private bool contextMenuShowHud = true;
        [Checkbox(description: "Context menu: Show HUD visibility toggle")]
        public bool ContextMenuShowHud
        {
            get => contextMenuShowHud;
            set => SetField(ref contextMenuShowHud, value);
        }

        private bool enableDoubleClick = false;
        [Checkbox(description: "Enable double-click actions on blocks")]
        public bool EnableDoubleClick
        {
            get => enableDoubleClick;
            set => SetField(ref enableDoubleClick, value);
        }

        private DoubleClickAction doubleClickAction = DoubleClickAction.None;
        [Dropdown(description: "Double-click action")]
        public DoubleClickAction DoubleClickAction
        {
            get => doubleClickAction;
            set => SetField(ref doubleClickAction, value);
        }
    }
}
