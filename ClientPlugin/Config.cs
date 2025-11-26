using ClientPlugin.Settings;
using ClientPlugin.Settings.Elements;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;


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
    }
}
