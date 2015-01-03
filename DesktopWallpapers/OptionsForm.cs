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
        List<string> _items = new List<string>(); 

        public OptionsForm()
        {
            InitializeComponent();
            _items = DesktopWallpapers.Program.settings.MPFiles;
            listBox1.DataSource = _items;            
        }

        private void button1_Click(object sender, EventArgs e)
        {
            //_items.Add("//");
        }

        private void button2_Click(object sender, EventArgs e)
        {
            DesktopWallpapers.Program.settings.Save(DesktopWallpapers.Program.SettingsFile);
            this.Close();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            var FD = new System.Windows.Forms.OpenFileDialog();
            if (FD.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                _items.Add(FD.FileName);
                listBox1.DataSource = null;
                listBox1.DataSource = _items;
                DesktopWallpapers.Program.settings.MPFiles = _items;
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            // The Remove button was clicked.
            int selectedIndex = listBox1.SelectedIndex;

            try
            {
                // Remove the item in the List.
                _items.RemoveAt(selectedIndex);
            }
            catch
            {
            }

            listBox1.DataSource = null;
            listBox1.DataSource = _items;
            DesktopWallpapers.Program.settings.MPFiles = _items;
        }

        private void button5_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
