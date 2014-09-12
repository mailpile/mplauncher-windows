using System;
using System.Drawing;
using System.Windows.Forms;
using System.Threading;

namespace Mailpile
{
    public class SplashForm : Form
    {
        private delegate void CloseDelegate();
        private static SplashForm splashForm;

        public SplashForm()
        {
            PictureBox pictureBox = new PictureBox();
            Label label = new Label();
            Image logo;

            try
            {
                logo = Image.FromFile("img\\logo.png");
            }
            catch (Exception e)
            {
                logo = null;
            }

            if (logo != null)
            {
                pictureBox.Image = logo;
                pictureBox.Height = logo.Height;
                pictureBox.Width = logo.Width;
            }

            label.Text = "Shutting down Mailpile";
            label.Width = logo.Width;
            label.TextAlign = ContentAlignment.MiddleCenter;
            label.Location = new Point(0, logo.Height - 30);
            label.Font = new Font("Tahoma", 12);

            this.Controls.Add(label);
            this.Controls.Add(pictureBox);
            this.Size = new Size(logo.Width, logo.Height + 20);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.None;
        }

        public static void ShowSplashScreen()
        {
            if (splashForm != null)
            {
                return;
            }

            Thread thread = new Thread(new ThreadStart(SplashForm.ShowForm));
            thread.IsBackground = true;

            try
            {
                thread.Start();
            }
            catch (Exception e)
            {
                //Unable to start splash form, fixme: log this and maybe show error message?
            }
        }

        public static void CloseSplashScreen()
        {
            if (splashForm == null)
            {
                return;
            }

            splashForm.Invoke(new CloseDelegate(SplashForm.CloseFormInternal));
        }

        private static void ShowForm()
        {
            splashForm = new SplashForm();
            Application.Run(splashForm);
        }

        private static void CloseFormInternal()
        {
            splashForm.Close();
            splashForm = null;
        }

        protected override bool ShowWithoutActivation
        {
            get { return true; }
        }
    }
}