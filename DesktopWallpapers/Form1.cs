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
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();

            GrabPictureFromBing();
            try
            {
                Image img = Image.FromFile(DesktopWallpapers.Program.LocalImageFilename);
                pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
                pictureBox1.Image = img;
                //pictureBox1.Height = 77*3;
                //pictureBox1.Width = 136*3;
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

            label1.Text = "Result from Bing: " + msg;
            DesktopWallpapers.Program.DownloadImage("http://www.bing.com" + url);
        }

        private void button1_Click(object sender, EventArgs e)
        {            
            this.Close();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            //Set the desktop background
            //label1.Text = "Old location: "+DesktopWallpapers.Program.GetWallpaper();
            DesktopWallpapers.Program.SetCurrentImageAsWallpaper();            
        }
    }
}
