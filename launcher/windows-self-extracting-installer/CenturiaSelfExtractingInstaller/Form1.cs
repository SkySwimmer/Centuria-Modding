using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CenturiaSelfExtractingInstaller
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            Task.Run(() =>
            {
                // Check for installer
                string exe = Application.ExecutablePath;
                Stream strm = File.OpenRead(exe);

                // Seek to header
                byte[] magic = Encoding.UTF8.GetBytes("CENTURIA!LAUNCHER");
                strm.Position = strm.Length - magic.Length - 8;
                long payloadEnd = strm.Position;

                // Read header
                byte[] pos = new byte[8];
                strm.Read(pos, 0, 8);
                if (!BitConverter.IsLittleEndian)
                    Array.Reverse(pos);

                // Read position
                long sPos = BitConverter.ToInt64(pos, 0);
                strm.Position = sPos;

                // Read archive
                Directory.CreateDirectory("installer-temp");
                Stream output = File.OpenWrite("installer-temp/data.zip");
                while (sPos < payloadEnd)
                {
                    byte[] block = new byte[2048];
                    if (payloadEnd - sPos < 2048)
                        block = new byte[payloadEnd - sPos];

                    // Read block
                    int read = strm.Read(block, 0, block.Length);
                    sPos += read;
                    output.Write(block, 0, block.Length);
                }
                output.Close();

                // Extract archive
                ZipArchive archive = new ZipArchive(File.OpenRead("installer-temp/data.zip"));
                Invoke(new Action(() =>
                {
                    label1.Text = "Extracting data...";
                    progressBar1.Style = ProgressBarStyle.Blocks;
                    progressBar1.Maximum = archive.Entries.Count;
                }));
                foreach (ZipArchiveEntry ent in archive.Entries)
                {
                    Invoke(new Action(() =>
                    {
                        progressBar1.Value++;
                    }));
                    if (ent.FullName.EndsWith("/") || ent.FullName.EndsWith("\\"))
                        continue;

                    // Read entry
                    Stream data = ent.Open();

                    // Create output
                    Directory.CreateDirectory(Path.GetDirectoryName("installer-temp/data/" + ent.FullName));
                    Stream dO = File.OpenWrite("installer-temp/data/" + ent.FullName);
                    data.CopyTo(dO);
                    data.Close();
                    dO.Close();
                }
                archive.Dispose();

                // Start launcher
                Invoke(new Action(() =>
                {
                    label1.Text = "Starting installer...";
                    progressBar1.Style = ProgressBarStyle.Marquee;
                }));
                string cp = "";
                foreach (FileInfo file in new DirectoryInfo("installer-temp/data/libs").GetFiles("*.jar"))
                {
                    if (cp != "")
                        cp += ";";
                    cp += "\"libs/" + file.Name + "\"";
                }
                foreach (FileInfo file in new DirectoryInfo("installer-temp/data").GetFiles("*.jar"))
                {
                    if (cp != "")
                        cp += ";";
                    cp += "\"" + file.Name + "\"";
                }
                Process proc = new Process();
                proc.StartInfo.UseShellExecute = false;
                proc.StartInfo.RedirectStandardOutput = true;
                proc.StartInfo.RedirectStandardError = true;
                proc.StartInfo.FileName = Path.GetFullPath("installer-temp/data/win/java-17/bin/javaw.exe");
                proc.StartInfo.WorkingDirectory = Path.GetFullPath("installer-temp/data");
                proc.StartInfo.Arguments = "-cp " + cp + " org.asf.centuria.launcher.updater.LauncherUpdaterMain " + Program.Arguments;
                proc.Start();
                Invoke(new Action(() => Hide()));
                proc.WaitForExit();
                Directory.Delete("installer-temp", true);
                Environment.Exit(proc.ExitCode);
            });
        }
    }
}
