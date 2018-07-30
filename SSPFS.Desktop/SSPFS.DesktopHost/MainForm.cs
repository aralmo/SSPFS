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

namespace SSPFS.DesktopHost
{
    public partial class MainForm : Form
    {

        string folder;
        public MainForm(string target_folder)
        {
            if (!Directory.Exists(target_folder))
                throw new DirectoryNotFoundException();

            folder = target_folder;
            InitializeComponent();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            Client.Current = new Client(folder);
            lbCarpeta.Text = folder;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (button1.Text == ">>")
            {
                this.Height = 264;
                button1.Text = "<<";
            }
            else
            {
                this.Height = 119;
                button1.Text = ">>";
            }
        }
    }
}
