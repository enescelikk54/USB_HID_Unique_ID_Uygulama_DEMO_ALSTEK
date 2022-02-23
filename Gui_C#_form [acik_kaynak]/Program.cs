using System;
using System.Windows.Forms;
using System.IO;
using System.Runtime.InteropServices;
using System.ComponentModel;

namespace AHidLib.Samples.Csharp
{
    static class Program
    {
        [DllImport("kernel32.dll", EntryPoint = "LoadLibraryW", ExactSpelling = true,
                    CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern IntPtr LoadLibrary(string path);

        public static void LoadUnmanagedDll()
        {
            string path = Path.Combine(Application.StartupPath, Environment.Is64BitProcess ? @"x64\" : @"Win32\", @"ahid.dll");

            IntPtr mod = LoadLibrary(path);

            if (mod == IntPtr.Zero)
                throw new Win32Exception();
        }

        /// <summary>
        /// Der Haupteinstiegspunkt für die Anwendung.
        /// </summary>
        [STAThread]
        static void Main()
        {
            LoadUnmanagedDll();

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }
    }
}
