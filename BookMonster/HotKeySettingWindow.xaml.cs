using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace BookMonster
{
    /// <summary>
    /// HotKeySettingWindow.xaml 的互動邏輯
    /// </summary>
    public partial class HotKeySettingWindow : Window
    {
        HotKey[] hotkeys;
        public HotKeySettingWindow()
        {
            InitializeComponent();
            hotkeys = (HotKey[])Savedata.shared.hotkeys.DeepClone();
            foreach (HotKey hotkey in hotkeys)
            {
                Grid item = makeHotKeyItem(hotkey);
                stack.Children.Add(item);
            }
        }

        Grid makeHotKeyItem(HotKey hotkey)
        {
            Grid grid = new Grid();
            grid.Height = 28;
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
            TextBlock title = new TextBlock();
            title.TextAlignment = TextAlignment.Center;
            title.VerticalAlignment = VerticalAlignment.Center;
            TextBox firstKey = new TextBox();
            firstKey.TextAlignment = TextAlignment.Center;
            firstKey.VerticalAlignment = VerticalAlignment.Center;
            Grid.SetColumn(firstKey, 1);
            TextBox secondKey = new TextBox();
            secondKey.TextAlignment = TextAlignment.Center;
            secondKey.VerticalAlignment = VerticalAlignment.Center;
            Grid.SetColumn(secondKey, 2);

            grid.Children.Add(title);
            grid.Children.Add(firstKey);
            grid.Children.Add(secondKey);

            title.Text = hotkey.getName;
            if (hotkey.keys.Length > 1)
            {
                firstKey.Text = hotkey.keys[0].ToString();
                secondKey.Text = hotkey.keys[1].ToString();
            }
            else if (hotkey.keys.Length > 0)
            {
                firstKey.Text = hotkey.keys[0].ToString();
            }
            firstKey.Tag = new object[] { hotkey, 0 };
            secondKey.Tag = new object[] { hotkey, 1 };
            firstKey.PreviewKeyDown += HotKey_PreviewKeyDown;
            secondKey.PreviewKeyDown += HotKey_PreviewKeyDown;
            firstKey.IsReadOnly = true;
            secondKey.IsReadOnly = true;
            return grid;
        }

        private void HotKey_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            TextBox box = (TextBox)sender;
            object[] info = (object[])box.Tag;
            HotKey hotkey = (HotKey)info[0];
            int index = (int)info[1];
            hotkey.keys[index] = e.Key;
            box.Text = hotkey.keys[index].ToString();
        }

        private void Reset_Clicked(object sender, RoutedEventArgs e)
        {
            hotkeys = Savedata.shared.initHotKeys();
            stack.Children.RemoveRange(1, hotkeys.Length);
            foreach (HotKey hotkey in hotkeys)
            {
                Grid item = makeHotKeyItem(hotkey);
                stack.Children.Add(item);
            }
        }

        private void Cancel_Clicked(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void OK_Clicked(object sender, RoutedEventArgs e)
        {
            Savedata.shared.hotkeys = this.hotkeys;
            Savedata.shared.needSave = true;
            this.Close();
        }
    }
}
