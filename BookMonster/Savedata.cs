using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;
using System.Management;
using System.IO;
using Newtonsoft.Json;
using System.Text.RegularExpressions;

namespace BookMonster
{
    public enum EventType
    {
        None,
        PrevPage,
        NextPage,
        PrevBook,
        NextBook,
        Escape,
        OpenWith,
        ScrollMode,
        Delete,
    }

    public struct HotKey : ICloneable
    {
        public EventType type;
        public Key[] keys;
        public HotKey(EventType type, Key[] keys)
        {
            this.type = type;
            this.keys = keys;
        }

        [JsonIgnore]
        public string getName
        {
            get
            {
                switch (type)
                {
                    case EventType.PrevPage:
                        return "上一頁";
                    case EventType.NextPage:
                        return "下一頁";
                    case EventType.PrevBook:
                        return "上一本";
                    case EventType.NextBook:
                        return "上一本";
                    case EventType.ScrollMode:
                        return "卷軸模式";
                    case EventType.OpenWith:
                        return "啟動其他看圖軟體";
                    case EventType.Escape:
                        return "退出";
                    case EventType.Delete:
                        return "刪除目前圖片";
                }
                return String.Empty;
            }
        }
        public object Clone()
        {
            HotKey hotkey = this;
            hotkey.keys = (Key[])this.keys.Clone();
            return hotkey;
        }
    }

    public class Savedata
    {
        static Savedata _shared;
        static public Savedata shared
        {
            get
            {
                if (_shared == null)
                {
                    if (!Savedata.load())
                    {
                        _shared = new Savedata();
                        _shared.needSave = true;
                        _shared.save();
                    }
                }
                return _shared;
            }
        }

        //public HotKey[] hotkeys;
        public Dictionary<EventType, HotKey> hotkeys;
        public bool scrollMode = false;
        public double wheelSpeed = 5;
        public int minCacheAmount = 3;
        private double _memoryLimit;
        public double memoryLimit
        {
            get
            {
                return _memoryLimit;
            }
            set
            {
                _memoryLimit = value;
                memoryLimitBytes = (long)(_memoryLimit * 1024.0 * 1024.0 * 1024.0);
            }
        }
        public string otherProgramPath = String.Empty;
        [JsonIgnore]
        public bool needSave = false;
        [JsonIgnore]
        public long memoryLimitBytes;

        public Savedata()
        {
            init();
            memoryLimit = Helper.getMemoryGb() / 2;
        }

        public void init()
        {
            hotkeys = initHotKeys();
        }

        public void save()
        {
            if (needSave)
            {
                string jsonString = JsonConvert.SerializeObject(_shared); 
                FileInfo file = new FileInfo(System.Reflection.Assembly.GetExecutingAssembly().Location);
                string path = String.Format("{0}\\savedata", file.Directory.FullName);
                File.WriteAllText(path, jsonString);
                needSave = false;
            }
        }
        static public bool load()
        {
            FileInfo file = new FileInfo(System.Reflection.Assembly.GetExecutingAssembly().Location);
            string path = String.Format("{0}\\savedata", file.Directory.FullName);
            if (!File.Exists(path))
            {
                return false;
            }
            string jsonString = File.ReadAllText(path);
            Savedata savedata;
            try
            {
                savedata = JsonConvert.DeserializeObject<Savedata>(jsonString);
            }
            catch (Exception ex)
            {
                return false;
            }
            Savedata._shared = savedata;
            return true;
        }

        public Dictionary<EventType, HotKey> initHotKeys()
        {
            HotKey[] keys = new HotKey[] {
                new HotKey(EventType.PrevPage, new Key[] { Key.Left, Key.None }),
                new HotKey(EventType.NextPage, new Key[] { Key.Right, Key.None }),
                new HotKey(EventType.PrevBook, new Key[] { Key.Up, Key.None }),
                new HotKey(EventType.NextBook, new Key[] { Key.Down ,Key.None }),
                new HotKey(EventType.ScrollMode, new Key[] { Key.S, Key.None } ),
                new HotKey(EventType.OpenWith, new Key[] { Key.Q, Key.None } ),
                new HotKey(EventType.Escape, new Key[] { Key.Escape ,Key.None }),
                new HotKey(EventType.Delete, new Key[] { Key.Delete,Key.None })
            };
            Dictionary<EventType, HotKey> hotkeys = new Dictionary<EventType, HotKey>();
            foreach (HotKey key in keys)
            {
                hotkeys.Add(key.type, key);
            }
            return hotkeys;
        }
        public EventType getEvent(Key input)
        {
            foreach (HotKey hotkey in hotkeys.Values)
            {
                if (hotkey.keys.Contains(input))
                {
                    return hotkey.type;
                }
            }
            return EventType.None;
        }
    }

    public static class Extension
    {
        public static T[] DeepClone<T>(this T[] source) where T : ICloneable
        {
            return source.Select(item => (T)item.Clone()).ToArray();
        }
        public static T[] RemoveAt<T>(this T[] source, int index)
        {
            List<T> list = new List<T>(source);
            list.RemoveAt(index);
            return list.ToArray();
        }
        public static Dictionary<EventType, HotKey> DeepClone(this Dictionary<EventType, HotKey> source)
        {
            Dictionary<EventType, HotKey> newdict = new Dictionary<EventType, HotKey>();
            foreach (HotKey key in source.Values)
            {
                newdict.Add(key.type, (HotKey)key.Clone());
            }
            return newdict;
        }
        public static string PadNumbers(this string input)
        {
            return Regex.Replace(input, "[0-9]+", match => match.Value.PadLeft(10, '0'));
        }
        static string[] imageExtension = { "jpg", "jpeg", "png", "gif", "bmp", "jfif" };
        public static bool isImage(this string extension)
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

        public static IEnumerable<FileInfo> getImageFiles(this DirectoryInfo folder)
        {
            return folder.GetFiles().Where(f => f.Name.isImage());
        }
        public static IOrderedEnumerable<T> OrderByFileName<T>(this IEnumerable<T> files) where T : FileSystemInfo
        {
            return files.OrderBy(f => f.Name.PadNumbers());
        }
    }
}
