using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DesktopWallpapers
{
    public partial class OptionsForm : Form
    {
        public OptionsForm()
        {
            InitializeComponent();
            textBox1.Text = DesktopWallpapers.Program.settings.MediaPortalSplashscreenFileLocation;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            var FD = new System.Windows.Forms.OpenFileDialog();
            if (FD.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                textBox1.Text = FD.FileName;
                DesktopWallpapers.Program.settings.MediaPortalSplashscreenFileLocation = FD.FileName;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            DesktopWallpapers.Program.settings.Save(DesktopWallpapers.Program.SettingsFile);
            this.Close();
        }
    }
}
