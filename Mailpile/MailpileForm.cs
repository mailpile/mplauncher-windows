using System;
using System.Configuration;
using System.Diagnostics;
using System.Drawing;
using System.Net;
using System.Threading;
using System.Windows.Forms;

namespace Mailpile
{
    public class MailpileForm : Form
    {
        private ContextMenu trayMenu;
        private MenuItem debugMenuItem;
        private NotifyIcon trayIcon;
        private Mailpile mailpile;
        private Thread processWatcher;

        public MailpileForm()
        {
            this.debugMenuItem = new MenuItem();
            this.trayMenu = new ContextMenu();
            this.trayIcon = new NotifyIcon();

            this.InitTray();
            this.StartMailpile();
            this.StartProcessWatcherThread();
        }

        private void InitTray()
        {
            var appSettings = ConfigurationManager.AppSettings;
            string[] keys = appSettings.AllKeys;

            foreach(string key in keys)
            {
                if(key.Equals("File") || key.Equals("Args") || key.Equals("REQ_Quit"))
                {
                    continue;
                }

                MenuItem item = new MenuItem();
                item.Name = key;
                item.Text = key.Contains("REQ_") ? key.Substring(4, key.Length - 4) : key;
                item.Click += new EventHandler(this.OnMenuItem_Click);
                this.trayMenu.MenuItems.Add(item);
            }

            this.debugMenuItem.Text = "Debug";
            this.debugMenuItem.Click += new EventHandler(this.OnDebug_Click);
            this.debugMenuItem.Checked = false;

            this.trayMenu.MenuItems.Add("-");
            this.trayMenu.MenuItems.Add(this.debugMenuItem);
            this.trayMenu.MenuItems.Add("-");
            this.trayMenu.MenuItems.Add("Quit", this.OnExit_Click);

            this.trayIcon.Icon = new Icon("img\\windows-mono-16x16.ico");
            this.trayIcon.Text = "Mailpile";
            this.trayIcon.ContextMenu = trayMenu;
            this.trayIcon.Visible = true;
        }

        private void StartMailpile()
        {
            bool ok = true;
            this.mailpile = new Mailpile();

            try
            {
                ok = this.mailpile.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unable to launch Mailpile." + Environment.NewLine +
                                "Exception: " + ex.Message, "Error",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            if (ok)
            {
                this.Notify("Mailpile is now running");
                Thread.Sleep(200); //give the mailpile process time to build console window
                this.mailpile.Debug(false);
                this.mailpile.DisableExit();
            }
        }

        private void StopMailpile()
        {
            bool ok = true;

            if (!this.mailpile.HasExited)
            {
                string value = ConfigurationManager.AppSettings["REQ_Quit"];

                if (!value.Equals(""))
                {
                    try
                    {
                        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(value);
                        ThreadPool.QueueUserWorkItem(o => { request.GetResponse(); });
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Unable to stop Mailpile." + Environment.NewLine +
                                        "Exception: " + ex.Message, "Error",
                                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                        ok = false;
                    }
                }
                else
                {
                    if(!this.mailpile.CloseMainWindow())
                    {
                        ok = false;
                    }
                }
            }

            if (ok)
            {
                this.mailpile.Close();
            }
        }

        private void Notify(string info)
        {
            this.trayIcon.BalloonTipIcon  = ToolTipIcon.Info;
            this.trayIcon.BalloonTipTitle = "Mailpile";
            this.trayIcon.BalloonTipText  = info;

            this.trayIcon.ShowBalloonTip(4000);
        }

        private void StartProcessWatcherThread()
        {
            this.processWatcher = new Thread(new ThreadStart(this.WatchProcess));

            try
            {
                this.processWatcher.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unable to start ProcessWatcherThread." + Environment.NewLine +
                                "Exception: " + ex.Message, "Error",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void StopProcessWatcherThread()
        {
            if (this.processWatcher != null)
            {
                try
                {
                    this.processWatcher.Abort();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Unable to stop ProcessWatcherThread" + Environment.NewLine +
                                     "Exception: " + ex.Message, "Error",
                                     MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void WatchProcess()
        {
            //Exit the program if mailpile is exited
            this.mailpile.WaitForExit();
            Application.Exit();
        }

        private void OnMenuItem_Click(object sender, EventArgs e)
        {
            MenuItem item = (MenuItem)sender;

            var appSettings = ConfigurationManager.AppSettings;
            string path = appSettings[item.Name];

            if (item.Name.Contains("REQ_"))
            {
                try
                {
                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(path);
                    ThreadPool.QueueUserWorkItem(o => { request.GetResponse(); });
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Unable to send request: " + path +
                                    Environment.NewLine + "Exception: " + ex.Message,
                                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                try
                {
                    Process.Start(path);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Unable to start process:" + path +
                                    Environment.NewLine + "Exception: " + ex.Message,
                                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void OnDebug_Click(object sender, EventArgs e)
        {
            if (!this.debugMenuItem.Checked)
            {
                this.mailpile.Debug(true);
                this.debugMenuItem.Checked = true;
            }
            else
            {
                this.mailpile.Debug(false);
                this.debugMenuItem.Checked = false;
            }
        }

        private void OnExit_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Are you sure you want to quit?", "Confirm quit", MessageBoxButtons.YesNo,
                MessageBoxIcon.Question) == DialogResult.Yes)
            {
                this.StopProcessWatcherThread();
                this.StopMailpile();
                Application.Exit();
            }
        }

        protected override void SetVisibleCore(bool value)
        {
            base.SetVisibleCore(false);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (this.trayIcon != null)
                {
                    this.trayIcon.Visible = false;
                    this.trayIcon.Icon = null;
                    this.trayIcon.Dispose();
                    this.trayIcon = null;
                }
            }

            base.Dispose(disposing);
        }

        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.Run(new MailpileForm());
        }
    }
}