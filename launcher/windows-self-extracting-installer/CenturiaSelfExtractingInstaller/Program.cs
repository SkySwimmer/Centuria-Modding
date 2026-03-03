using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CenturiaSelfExtractingInstaller
{
    static class Program
    {
        public static string Arguments = "";

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            foreach (string arg in args)
            {
                if (Arguments != "")
                    Arguments += " ";
                if (arg.Contains(" ") || arg.Contains("\""))
                    Arguments += "\"" + arg.Replace("\\\"", "\\\\\"").Replace("\"", "\\\"");
                else
                    Arguments += arg;
            }

            // Start app
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }
    }
}
