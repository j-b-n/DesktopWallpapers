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
                                                 
            GrabPictureFromBing();
            try
            {
                Image img = FromFile(DesktopWallpapers.Program.settings.LocalImageFilename);                
                pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
                pictureBox1.Image = img;
                //pictureBox1.Height = 77*3;
                //pictureBox1.Width = 136*3;                
                toolStripStatusLabel3.Text = "Last update: "+DesktopWallpapers.Program.getLocalImageDate();
            }
            catch (Exception e)
            {
                DesktopWallpapers.Program.ClearCache();
               
            }                       
        }

        public void GrabPictureFromBing()
        {
            string url = "";
            string msg = "";

            DesktopWallpapers.Program.DownloadBingXML();
            DesktopWallpapers.Program.LoadXML();

            // Write values to dialog box.

            foreach (DesktopWallpapers.Program.BingImage BI in DesktopWallpapers.Program.BingImages)
            {
                if (BI.url == null)
                {
                    msg = "No URL found!";
                }
                else
                {
                    msg = BI.url;
                    url = BI.url;
                }

                if (BI.copyright == null)
                {
                    msg = "No copyright found!";
                }
                else
                {
                    msg = BI.copyright;
                }
            }
            
            toolStripStatusLabel1.Text = msg.Trim();
            DesktopWallpapers.Program.DownloadImage("http://www.bing.com" + url);
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
            pictureBox1.InitialImage = null;
            pictureBox1.Image = null;
            pictureBox1.Refresh();

            DesktopWallpapers.Program.ClearCache();            
            DesktopWallpapers.Program.DownloadBingXML();
            
            // Write values to dialog box.
            string msg = "";
            string url = "";
            foreach (DesktopWallpapers.Program.BingImage BI in DesktopWallpapers.Program.BingImages)
            {
                if (BI.url == null)
                {
                    msg = "No URL found!";
                }
                else
                {
                    msg = BI.url;
                    url = BI.url;
                }

                if (BI.copyright == null)
                {
                    msg = "No copyright found!";
                }
                else
                {
                    msg = BI.copyright;
                }
            }

            GrabPictureFromBing();
            try
            {
                Image img = FromFile(DesktopWallpapers.Program.settings.LocalImageFilename);
                pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
                pictureBox1.Image = img;
                //pictureBox1.Height = 77*3;
                //pictureBox1.Width = 136*3;                
                toolStripStatusLabel3.Text = "Last update: " + DesktopWallpapers.Program.getLocalImageDate();
                DesktopWallpapers.Program.SetCurrentImageAsWallpaper();
            }
            catch (Exception exp)
            {
                DesktopWallpapers.Program.ClearCache();

            }
        }

        private void setMPToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DesktopWallpapers.Program.SetCurrentImageAsWallpaper();            

            
        }
    }
}
