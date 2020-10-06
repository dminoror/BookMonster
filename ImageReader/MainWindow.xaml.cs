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

namespace BookMonster
{
    public partial class MainWindow : Window
    {
        App app = (App)App.Current;
        DirectoryInfo currentFolder;
        FileInfo[] currentFiles;
        ulong memoryLimit;
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
            
            memoryLimit = (memorySize / 20);
        }

        BitmapImage[] images;
        int index = 0;

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

        string[] imageExtension = { "jpg", "jpeg", "png", "gif", "bmp" };
        bool isImage(string extension)
        {
            extension = extension.ToLower();
            foreach (string ext in imageExtension)
            {
                if (extension.Contains(ext))
                {
                    return true;
                }
            }
            return false;
        }

        void inputFile(string filePath)
        {
            resetImages();
            FileInfo file = new FileInfo(filePath);
            DirectoryInfo folder = file.Directory;
            currentFiles = folder.GetFiles();
            currentFiles = currentFiles.Where(f => isImage(f.Extension)).ToArray();
            index = Array.FindIndex(currentFiles, f => f.FullName == file.FullName);
            currentFolder = folder;
            images = new BitmapImage[currentFiles.Length];
            abortThread();
            loadFiles();
        }
        void inputFolder(DirectoryInfo folder)
        {
            resetImages();
            currentFiles = folder.GetFiles();
            index = 0;
            currentFolder = folder;
            currentFiles = currentFiles.Where(f => isImage(f.Extension)).ToArray();
            images = new BitmapImage[currentFiles.Length];
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
            if (readThread != null) { return; }
            Console.WriteLine("start thread");
            readThread = new Thread(() =>
            {
                for (int i = index; i < currentFiles.Length; i++)
                {
                    if (images[i] == null)
                    {
                        images[i] = readImage(i, currentFiles[i]);
                        if (checkMemory()) {
                            cleanMemory();
                            abortThread();
                        }
                    }
                }
                for (int i = index - 1; i >= 0; i--)
                {
                    if (images[i] == null)
                    {
                        images[i] = readImage(i, currentFiles[i]);
                        if (checkMemory()) {
                            cleanMemory();
                            abortThread();
                        }
                    }
                }
                Console.WriteLine("end");
                abortThread();
            });
            readThread.Start();
        }
        void abortThread()
        {
            if (readThread != null)
            {
                Thread thread = readThread;
                readThread = null;
                thread.Abort();
            }
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
            if (index == this.index)
            {
                this.Dispatcher.BeginInvoke(new Action(() =>
                {
                    renderImage();
                }));
            }
            return bitmap;
        }

        bool checkMemory()
        {
            var memory = (ulong)GC.GetTotalMemory(true);
            return memory > memoryLimit;
        }
        void cleanMemory()
        {
            Console.WriteLine("start clean memory");
            for (int i = 0; i < index - 1; i++)
            {
                if (images[i] != null)
                {
                    images[i] = null;
                    Console.WriteLine("remove index " + i);
                    var memory = (ulong)GC.GetTotalMemory(true);
                    if (memory < memoryLimit) {
                        Console.WriteLine("clean finish");
                        return; 
                    }
                }
            }
            for (int i = images.Length - 1; i > index; i--)
            {
                if (images[i] != null)
                {
                    images[i] = null;
                    Console.WriteLine("remove index " + i);
                    var memory = (ulong)GC.GetTotalMemory(true);
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
            if (index < images.Length && index >= 0)
                imageMain.Source = images[index];
            this.Title = string.Format("{0} ({1} / {2})", currentFolder.Name, index + 1, images.Length);
        }

        private void window_KeyDown(object sender, KeyEventArgs e)
        {
            if (images != null)
            {
                if (e.Key == Key.Right)
                {
                    if (index < images.Length - 1)
                    {
                        index += 1;
                    }
                    renderImage();
                    loadFiles();
                    return;
                }
                else if (e.Key == Key.Left)
                {
                    if (index > 0)
                    {
                        index -= 1;
                    }
                    renderImage();
                    loadFiles();
                    return;
                }
                else if (e.Key == Key.Up)
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
                    return;
                }
                else if (e.Key == Key.Down)
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
                    return;
                }
            }
            
            if (e.Key == Key.Escape)
            {
                this.Close();
            }
            else if (e.Key == Key.R)
            {
                loadFiles();
            }
        }

        private void window_Closed(object sender, EventArgs e)
        {
            abortThread();
        }
    }
}
