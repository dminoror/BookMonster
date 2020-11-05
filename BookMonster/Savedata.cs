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
        Escape
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
                    case EventType.Escape:
                        return "退出";
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

        public HotKey[] hotkeys;
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
                string path = String.Format("{0}\\savedata", Environment.CurrentDirectory);
                File.WriteAllText(path, jsonString);
                needSave = false;
            }
        }
        static public bool load()
        {
            string path = String.Format("{0}\\savedata", Environment.CurrentDirectory);
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

        public HotKey[] initHotKeys()
        {
            HotKey[] hotkeys = new HotKey[] {
                new HotKey(EventType.PrevPage, new Key[] { Key.Left, Key.None }),
                new HotKey(EventType.NextPage, new Key[] { Key.Right, Key.None }),
                new HotKey(EventType.PrevBook, new Key[] { Key.Up, Key.None }),
                new HotKey(EventType.NextBook, new Key[] { Key.Down ,Key.None }),
                new HotKey(EventType.Escape, new Key[] { Key.Escape ,Key.None })
            };
            return hotkeys;
        }
        public EventType getEvent(Key input)
        {
            foreach (HotKey hotkey in hotkeys)
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
    }
}
