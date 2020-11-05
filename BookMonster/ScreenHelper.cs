using BookMonster.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Shapes;

namespace BookMonster
{
    public static class Helper
    {
        public static Screen getOpenScreen()
        {
            var screen = Screen.FromPoint(Cursor.Position);
            return screen;
        }

        public static double getMemoryGb()
        {
            ObjectQuery wql = new ObjectQuery("SELECT * FROM Win32_OperatingSystem");
            ManagementObjectSearcher searcher = new ManagementObjectSearcher(wql);
            ManagementObjectCollection results = searcher.Get();

            foreach (ManagementObject result in results)
            {
                var memory = (UInt64)result["TotalVisibleMemorySize"];
                if (memory > 0)
                {
                    return Math.Round(memory / 1024.0 / 1024.0);
                }
            }
            return 0;
        }
    }
}
