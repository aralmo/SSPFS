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
        FileSystemWatcher watcher;
        string folder;
        public MainForm(string target_folder)
        {
            if (!Directory.Exists(target_folder))
                throw new DirectoryNotFoundException();

            folder = target_folder;

            watcher = new FileSystemWatcher(target_folder);
            watcher.EnableRaisingEvents = true;
            watcher.Created += Watcher_Changed;
            watcher.Deleted += Watcher_Changed;

            InitializeComponent();
        }

        private void Watcher_Changed(object sender, FileSystemEventArgs e)
        {
            //enviar una señal al servidor para aactualizar por signalr 
            Client.Current.ReportDirectoryChangesToServer();
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
                this.Height = 292;
                button1.Text = "<<";
            }
            else
            {
                this.Height = 146;
                button1.Text = ">>";
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Clipboard.SetText(tburl.Text);
        }

        private void cbPermitirSubida_CheckedChanged(object sender, EventArgs e)
        {
            //para que se esconda o muestre el botón subir ficheros según el estado del check.
            Client.Current.ReportDirectoryChangesToServer();
        }
    }
}
