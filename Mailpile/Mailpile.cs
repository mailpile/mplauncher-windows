using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace Mailpile
{
    public class Mailpile : Process
    {
        private const uint SC_CLOSE = 0xf060;
        private const uint MF_GRAYED = 0x01;

        public Mailpile()
        {
            var appSettings = ConfigurationManager.AppSettings;

            this.StartInfo.FileName = appSettings["File"];
            this.StartInfo.Arguments = appSettings["Args"];
            this.StartInfo.UseShellExecute = true;
        }

        public void Debug(bool debug)
        {
            if (this.HasExited)
            {
                return;
            }
            else
            {
                if (this.MainWindowHandle != IntPtr.Zero)
                {
                    if (debug)
                    {
                        ShowWindow(this.MainWindowHandle, 1);
                    }
                    else
                    {
                        ShowWindow(this.MainWindowHandle, 0);
                    }
                }
            }
        }

        public void DisableExit()
        {
            IntPtr hMenu = GetSystemMenu(this.MainWindowHandle, false);
            EnableMenuItem(hMenu, SC_CLOSE, MF_GRAYED);
        }

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);

        [DllImport("user32.dll")]
        private static extern int EnableMenuItem(IntPtr hMenu, uint wIDEnableItem, uint wEnable);
    }
}