using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DesktopWallpapers
{
    public partial class Form1 : Form
    {

        public static Image FromFile(string path)
        {
            var bytes = File.ReadAllBytes(path);
            var ms = new MemoryStream(bytes);
            var img = Image.FromStream(ms);
            return img;
        }

        public Form1()
        {
            InitializeComponent();

            DesktopWallpapers.Program.refreshBing(false);

            try
            {
                Image img = FromFile(DesktopWallpapers.Program.settings.LocalImageFilename);                
                pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
                pictureBox1.Image = img;                
                toolStripStatusLabel2.Text = "Last update: "+DesktopWallpapers.Program.getLocalImageDate();
            }
            catch (Exception e)
            {
                DesktopWallpapers.Program.ClearCache();
                DesktopWallpapers.Program.Log.Error(e.Message);
            }                       
        }
        

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void setToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DesktopWallpapers.Program.SetCurrentImageAsWallpaper();            
        }

        private void optionsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OptionsForm OF = new OptionsForm();
            OF.ShowDialog();
        }

        private void forceUpdateToolStripMenuItem_Click(object sender, EventArgs e)
        {            
            DesktopWallpapers.Program.ClearCache();
            DesktopWallpapers.Program.refreshBing(true);
            //DesktopWallpapers.Program.SetCurrentImageAsWallpaper();            
        }

        private void setMPToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DesktopWallpapers.Program.SetCurrentImageAsWallpaper();
            DesktopWallpapers.Program.SaveMPFiles();            
        }
    }
}
