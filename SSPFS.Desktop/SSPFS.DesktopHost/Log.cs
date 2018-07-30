using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SSPFS.DesktopHost
{
    public static class Log
    {
        static string last_message = string.Empty;
        public static void LogMessage(string message)
        {
            if (message == last_message)
            {
                return;
            }
            else
            {
                last_message = message;
            }

            if (Program.Form == null)
                Console.WriteLine(message);
            else
            {
                Program.Form.Invoke(new MethodInvoker(() =>
                {
                    Program.Form.tbLog.Text += message + "\r\n\r\n";
                }));
            }
        }
        public static void LogError(string message)
        {
            LogMessage("Error: " + message);
        }
    }
}
