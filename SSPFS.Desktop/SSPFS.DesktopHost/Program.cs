using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SSPFS.DesktopHost
{
    static class Program
    {
        public static MainForm Form;
        /// <summary>
        /// Punto de entrada principal para la aplicación.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            string folder = @"C:\Users\aralm\Desktop\test_folder";

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Form = new MainForm(folder);
            Application.Run(Form);
        }
    }
}
