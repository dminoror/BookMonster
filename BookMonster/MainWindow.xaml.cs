using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using System.Windows.Threading;
using System.Threading;
using System.Windows.Media.Animation;
using System.Diagnostics;

namespace BookMonster
{
    public partial class MainWindow : Window
    {
        App app = (App)App.Current;
        DirectoryInfo currentFolder;
        FileInfo[] currentFiles;
        long memoryLimit;
        int minCache = 3;

        Savedata savedata = Savedata.shared;
        public bool scrollMode
        {
            get { return savedata.scrollMode; }
            set { savedata.scrollMode = value; }
        }
        
        public MainWindow()
        {
            InitializeComponent();
            System.Windows.Forms.Screen screen = Helper.getOpenScreen();
            window.WindowStartupLocation = WindowStartupLocation.Manual;
            window.Left = screen.Bounds.X;
            window.Top = screen.Bounds.Y;
            window.Width = screen.Bounds.Width;
            window.Height = screen.Bounds.Height;
            app.MainWindow.WindowState = WindowState.Maximized;
            ulong memorySize = (ulong)Helper.getMemory();
            memoryLimit = (long)memorySize / 2;
            Console.WriteLine("Memory limit is {0} Mb", memoryLimit / 1024.0 / 1024.0);
            setRendorMode(scrollMode);
        }

        BitmapImage[] images;
        Image[] imageViews;
        volatile bool forceAbort;
        volatile int index = 0;

        private void window_Loaded(object sender, RoutedEventArgs e)
        {
            if (app.openPath.Length > 0)
            {
                inputFile(app.openPath);
                loadFiles();
            }
        }
        private void window_Drop(object sender, DragEventArgs e)
        {
            var paths = ((System.Array)e.Data.GetData(DataFormats.FileDrop));
            if (paths.Length > 0)
            {
                string path = paths.GetValue(0).ToString();
                if (File.Exists(path))
                {
                    inputFile(path);
                }
                else if (Directory.Exists(path))
                {
                    inputFolder(new DirectoryInfo(path));
                }
            }
        }

        void inputFile(string filePath)
        {
            resetImages();
            FileInfo file = new FileInfo(filePath);
            DirectoryInfo folder = file.Directory;
            currentFiles = folder.GetFiles().Where(f => f.Name.isImage()).OrderBy(f => f.Name.PadNumbers()).ToArray();
            index = Array.FindIndex(currentFiles, f => f.FullName == file.FullName);
            if (index < 0) { index = 0; }
            currentFolder = folder;
            images = new BitmapImage[currentFiles.Length];
            setRendorMode(scrollMode);
            abortThread();
            loadFiles();
        }
        void inputFolder(DirectoryInfo folder)
        {
            resetImages();
            index = 0;
            currentFiles = folder.GetFiles().Where(f => f.Name.isImage()).OrderBy(f => f.Name.PadNumbers()).ToArray();
            currentFolder = folder;
            images = new BitmapImage[currentFiles.Length];
            setRendorMode(scrollMode);
            abortThread();
            loadFiles();
        }

        void resetImages()
        {
            currentFiles = null;
            images = null;
            imageMain.Source = null;
        }

        Thread readThread;
        void loadFiles()
        {
            if (readThread != null) 
            {
                return; 
            }
            forceAbort = false;
            readThread = new Thread(() =>
            {
                while (true)
                {
                    if (forceAbort) { break; }
                    int loadingIndex = getNextLoadIndex(index);
                    if (loadingIndex < 0) { break; }
                    images[loadingIndex] = readImage(loadingIndex, currentFiles[loadingIndex]);
                    if (checkMemory())
                    {
                        cleanMemory();
                        bool loadNext = false;
                        for (int i = index; i < index + minCache; i++)
                        {
                            if (i < images.Length && images[i] == null)
                            {
                                loadNext = true;
                                break;
                            }
                        }
                        if (!loadNext)
                            break;
                    }
                }
                disposeThread();
            });
            readThread.Start();
        }

        void disposeThread()
        {
            if (readThread != null)
            {
                Thread thread = readThread;
                readThread = null;
                thread.Abort();
            }
        }
        void abortThread()
        {
            if (readThread != null)
            {
                forceAbort = true;
                while (readThread != null)
                {

                }
                forceAbort = false;
            }
        }
        int getNextLoadIndex(int currentIndex)
        {
            for (int i = currentIndex; i < currentFiles.Length; i++)
            {
                if (images[i] == null)
                {
                    return i;
                }
            }
            for (int i = currentIndex - 1; i >= 0; i--)
            {
                if (images[i] == null)
                {
                    return i;
                }
            }
            return -1;
        }

        BitmapImage readImage(int index, FileInfo file)
        {
            Console.WriteLine("start read " + file.Name);
            BitmapImage bitmap = new BitmapImage();
            using (FileStream stream = new FileStream(file.FullName, FileMode.Open))
            {
                bitmap.BeginInit();
                bitmap.StreamSource = stream;
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                stream.Close();
                bitmap.Freeze();
            }
            if (scrollMode)
            {
                int loadedIndex = index;
                this.Dispatcher.BeginInvoke(new Action(() =>
                {
                    imageViews[loadedIndex].Source = images[loadedIndex];
                }));
            }
            else if (index == this.index)
            {
                this.Dispatcher.BeginInvoke(new Action(() =>
                {
                    renderImage();
                }));
            }
            return bitmap;
        }
        long currentMemoryUsage
        {
            get
            {
                Process currentProc = Process.GetCurrentProcess();
                return currentProc.PrivateMemorySize64;
            }
        }
        bool checkMemory()
        {
            long memory = currentMemoryUsage;
            Console.WriteLine("Current memory is {0} Mb", memory / 1024.0 / 1024.0);
            return memory > memoryLimit;
        }
        void cleanMemory()
        {
            Console.WriteLine("start clean memory");
            for (int i = 0; i < index - minCache; i++)
            {
                if (images[i] != null)
                {
                    images[i] = null;
                    if (scrollMode)
                    {
                        this.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            imageViews[i].Source = null;
                        }));
                    }
                    Console.WriteLine("remove index " + i);
                    GC.Collect();
                    var memory = currentMemoryUsage;
                    if (memory < memoryLimit)
                    {
                        Console.WriteLine("clean finish");
                        return;
                    }
                }
            }
            for (int i = images.Length - 1; i > index + minCache; i--)
            {
                if (images[i] != null)
                {
                    images[i] = null;
                    if (scrollMode)
                    {
                        this.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            imageViews[i].Source = null;
                        }));
                    }
                    Console.WriteLine("remove index " + i);
                    GC.Collect();
                    var memory = currentMemoryUsage;
                    if (memory < memoryLimit)
                    {
                        Console.WriteLine("clean finish");
                        return;
                    }
                }
            }
        }

        void renderImage()
        {
            if (!scrollMode)
            {
                if (index < images.Length && index >= 0)
                {
                    imageMain.Source = images[index];
                }
            }
            this.Title = string.Format("{0} ({1} / {2})", currentFolder.Name, index + 1, images.Length);
            this.TaskbarItemInfo.ProgressValue = (index + 1) / (double)images.Length;
        }

        void setRendorMode(bool scrollMode)
        {
            this.scrollMode = scrollMode;
            scrollModeChecked.IsChecked = scrollMode;
            if (scrollMode)
            {
                normalModeView.Opacity = 0;
                scrollModeView.Opacity = 1;
                setupScroll();
            }
            else
            {
                normalModeView.Opacity = 1;
                scrollModeView.Opacity = 0;
                imageViews = null;
                scroll.Children.Clear();
                if (images != null && index < images.Length)
                    imageMain.Source = images[index];
            }
        }
        void setupScroll()
        {
            if (!scrollMode || images == null || images.Length == 0) { return; }
            scroll.Children.Clear();
            imageViews = new Image[images.Length];
            for (int i = 0; i < images.Length; i++)
            {
                Image image = new Image();
                RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.Fant);
                image.Stretch = Stretch.Uniform;
                image.Width = parentView.ActualWidth;
                image.Height = parentView.ActualHeight;
                scroll.Children.Add(image);
                imageViews[i] = image;
                image.Source = images[i];
            }
        }

        private void window_KeyDown(object sender, KeyEventArgs e)
        {
            EventType type = savedata.getEvent(e.Key);
            switch (type)
            {
                case EventType.PrevPage:
                    {
                        if (images == null || scrollMode) return;
                        if (index > 0)
                        {
                            index -= 1;
                        }
                        else
                        {
                            leftSide.BeginAnimation(UIElement.OpacityProperty, null);
                            leftSide.Opacity = 1;
                            var animation = new DoubleAnimation
                            {
                                To = 0,
                                BeginTime = TimeSpan.FromSeconds(0),
                                Duration = TimeSpan.FromSeconds(0.5),
                                FillBehavior = FillBehavior.Stop
                            };
                            animation.Completed += (s, a) => leftSide.Opacity = 0;
                            leftSide.BeginAnimation(UIElement.OpacityProperty, animation);
                        }
                        renderImage();
                        loadFiles();
                    }
                    break;
                case EventType.NextPage:
                    {
                        if (images == null || scrollMode) return;
                        if (index < images.Length - 1)
                        {
                            index += 1;
                        }
                        else
                        {
                            rightSide.BeginAnimation(UIElement.OpacityProperty, null);
                            rightSide.Opacity = 1;
                            var animation = new DoubleAnimation
                            {
                                To = 0,
                                BeginTime = TimeSpan.FromSeconds(0),
                                Duration = TimeSpan.FromSeconds(0.5),
                                FillBehavior = FillBehavior.Stop
                            };
                            animation.Completed += (s, a) => rightSide.Opacity = 0;
                            rightSide.BeginAnimation(UIElement.OpacityProperty, animation);
                        }
                        renderImage();
                        loadFiles();
                    }
                    break;
                case EventType.PrevBook:
                    {
                        DirectoryInfo parent = currentFolder.Parent;
                        DirectoryInfo[] folders = parent.GetDirectories();
                        int index = Array.FindIndex(folders, folder => folder.FullName == currentFolder.FullName);
                        if (index == 0)
                        {
                            MessageBox.Show("這是第一個資料夾");
                        }
                        else
                        {
                            inputFolder(folders[index - 1]);
                        }
                    }
                    break;
                case EventType.NextBook:
                    {
                        DirectoryInfo parent = currentFolder.Parent;
                        DirectoryInfo[] folders = parent.GetDirectories();
                        int index = Array.FindIndex(folders, folder => folder.FullName == currentFolder.FullName);
                        if (index == folders.Length - 1)
                        {
                            MessageBox.Show("這是最後一個資料夾");
                        }
                        else
                        {
                            inputFolder(folders[index + 1]);
                        }
                    }
                    break;
                case EventType.Escape:
                    {
                        this.Close();
                    }
                    break;
            }
        }

        private void window_Closed(object sender, EventArgs e)
        {
            disposeThread();
            Savedata.save();
        }

        private void HotKeySetting_Click(object sender, RoutedEventArgs e)
        {
            HotKeySettingWindow window = new HotKeySettingWindow();
            window.ShowDialog();
        }

        private void scrollMode_Clicked(object sender, RoutedEventArgs e)
        {
            setRendorMode(!scrollMode);
        }

        private void scrollModeView_Scrolled(object sender, ScrollChangedEventArgs e)
        {
            if (images != null && scrollMode)
            {
                int scrollIndex = (int)(scrollModeView.VerticalOffset / scroll.ActualHeight * images.Length);
                if (scrollIndex != index)
                {
                    index = scrollIndex;
                    loadFiles();
                    Console.WriteLine("scrolled");
                }
            }
        }

        private void scroll_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            scrollModeView.ScrollToVerticalOffset(scrollModeView.VerticalOffset - e.Delta * savedata.wheelSpeed);
            e.Handled = true;
        }
    }
}
