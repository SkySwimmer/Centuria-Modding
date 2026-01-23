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
            bool hasInstallerData = true;
            foreach (string arg in args)
            {
                if (Arguments != "")
                    Arguments += " ";
                if (arg.Contains(" ") || arg.Contains("\""))
                    Arguments += "\"" + arg.Replace("\\\"", "\\\\\"").Replace("\"", "\\\"");
                else
                    Arguments += arg;
            }

            // Check for installer
            string exe = Application.ExecutablePath;
            Stream strm = File.OpenRead(exe);

            // Seek to end
            byte[] magic = Encoding.UTF8.GetBytes("CENTURIA!LAUNCHER");
            strm.Position = strm.Length - magic.Length;

            // Read
            byte[] current = new byte[magic.Length];
            int read = strm.Read(current, 0, current.Length);

            // Verify
            for (int i = 0; i < current.Length; i++)
                if (current[i] != magic[i])
                {
                    hasInstallerData = false;
                    break;
                }

            // Check arguments
            if (args.Length >= 1 && !hasInstallerData)
            {
                if (!File.Exists(args[0]))
                {
                    MessageBox.Show("Invalid argument: expected a zip file", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Environment.Exit(1);
                }

                Stream str = File.OpenRead(args[0]);
                try
                {
                    ZipArchive a = new ZipArchive(str);
                    a.Entries.ToArray();
                    a.Dispose();
                    str = File.OpenRead(args[0]);
                }
                catch
                {
                    MessageBox.Show("Invalid argument: expected a zip file", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Environment.Exit(1);
                }

                // Copy data
                Console.WriteLine("Copying zip into installer...");
                FileStream o = File.OpenWrite(args.Length == 1 ? "installer.exe" : args[1]);
                strm.Position = 0;
                strm.CopyTo(o);

                // Write data
                long pos = o.Position;
                str.CopyTo(o);
                str.Close();

                // Write headers
                byte[] h = BitConverter.GetBytes(pos);
                if (!BitConverter.IsLittleEndian)
                    Array.Reverse(h);
                o.Write(h, 0, h.Length);
                o.Write(magic, 0, magic.Length);

                // Close output
                o.Close();
                Environment.Exit(0);
            }

            // Close stream
            strm.Close();
            if (!hasInstallerData)
            { 
                MessageBox.Show("No data present in this installer!", "", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Environment.Exit(1);
            }

            // Start app
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }
    }
}
