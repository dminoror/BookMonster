using System.IO;
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
            tbWheelSpeed.Text = Savedata.shared.wheelSpeed.ToString("F1");
            tbCacheAmount.Text = Savedata.shared.minCacheAmount.ToString();
            tbMemoryLimit.Text = Savedata.shared.memoryLimit.ToString();
            System.Drawing.Point point = System.Windows.Forms.Control.MousePosition;
            this.Left = point.X - this.Width / 2;
            this.Top = point.Y - this.Height / 2;
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
            Savedata newdata = new Savedata();
            hotkeys = newdata.hotkeys;
            stack.Children.RemoveRange(stack.Children.IndexOf(gridHotkeysSection) + 1, hotkeys.Length);
            foreach (HotKey hotkey in hotkeys)
            {
                Grid item = makeHotKeyItem(hotkey);
                stack.Children.Add(item);
            }
            tbWheelSpeed.Text = newdata.wheelSpeed.ToString("F1");
            tbCacheAmount.Text = newdata.minCacheAmount.ToString();
            tbMemoryLimit.Text = newdata.memoryLimit.ToString();
        }

        private void Cancel_Clicked(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void openOther_Clicked(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.OpenFileDialog dialog = new System.Windows.Forms.OpenFileDialog();
            dialog.Filter = "執行檔 (*.exe)|*.exe";
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                tbOtherProgram.Text = dialog.FileName;
            }
        }

        private void OK_Clicked(object sender, RoutedEventArgs e)
        {
            double wheelSpeed = 0;
            if (!double.TryParse(tbWheelSpeed.Text, out wheelSpeed))
            {
                MessageBox.Show("滑鼠速度輸入有誤"); return;
            }
            if (wheelSpeed < 0.1)
            {
                MessageBox.Show("滑鼠速度太低"); return;
            }
            int cacheAmount = 0;
            if (!int.TryParse(tbCacheAmount.Text, out cacheAmount) || cacheAmount < 0)
            {
                MessageBox.Show("最少快取量輸入有誤"); return;
            }
            double memoryLimit = 0;
            if (!double.TryParse(tbMemoryLimit.Text, out memoryLimit) || memoryLimit < 0)
            {
                MessageBox.Show("記憶體上限輸入有誤"); return;
            }
            if (tbOtherProgram.Text.Length > 0 && !File.Exists(tbOtherProgram.Text))
            {
                MessageBox.Show("看圖軟體路徑有誤"); return;
            }
            Savedata.shared.wheelSpeed = wheelSpeed;
            Savedata.shared.minCacheAmount = cacheAmount;
            Savedata.shared.memoryLimit = memoryLimit;
            Savedata.shared.otherProgramPath = tbOtherProgram.Text;
            Savedata.shared.hotkeys = this.hotkeys;
            Savedata.shared.needSave = true;
            Savedata.shared.save();
            this.Close();
        }
    }
}
