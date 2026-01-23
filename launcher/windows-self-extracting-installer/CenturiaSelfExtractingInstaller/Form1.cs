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
                long sPos = 0;
                long payloadEnd = 0;
                Stream strm = null;
                try
                {
                    Invoke(new Action(() =>
                    {
                        label1.Text = "Processing installer...";
                    }));

                    // Check for installer
                    string exe = Application.ExecutablePath;
                    strm = File.OpenRead(exe);

                    // Seek to header
                    byte[] magic = Encoding.UTF8.GetBytes("CENTURIA!LAUNCHER");
                    strm.Position = strm.Length - magic.Length - 8;
                    payloadEnd = strm.Position;

                    // Read header
                    byte[] pos = new byte[8];
                    strm.Read(pos, 0, 8);
                    if (!BitConverter.IsLittleEndian)
                        Array.Reverse(pos);

                    // Read position
                    sPos = BitConverter.ToInt64(pos, 0);
                    strm.Position = sPos;
                }
                catch
                {
                    if (strm != null)
                        strm.Close();
                    MessageBox.Show("An error occurred while trying to extract or run the installer, unable to continue!\n\nThe installer could not locate the payload data, please try verifying if the download you received is not damaged.", "An error occurred", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Environment.Exit(1);
                    return;
                }

                string path = Path.GetTempPath() + "/installer-temp-" + DateTimeOffset.UtcNow.ToUnixTimeSeconds() + "-" + Guid.NewGuid().ToString("D");
                try
                {
                    Invoke(new Action(() =>
                    {
                        label1.Text = "Copying installer contents to temporary location...";
                    }));

                    // Read archive
                    Directory.CreateDirectory(path);
                    Stream output = File.OpenWrite(path + "/data.zip");
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
                }
                catch
                {
                    if (strm != null)
                        strm.Close();
                    try
                    {
                        Directory.Delete(path, true);
                    }
                    catch
                    {}
                    MessageBox.Show("An error occurred while trying to extract or run the installer, unable to continue!\n\nFailed to copy the installer content zip to temporary installer folder! Please contact support for the program you are trying to install.", "An error occurred", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Environment.Exit(1);
                    return;
                }

                try
                {
                    // Extract archive
                    ZipArchive archive = new ZipArchive(File.OpenRead(path + "/data.zip"));
                    Invoke(new Action(() =>
                    {
                        label1.Text = "Extracting installer...";
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
                        Directory.CreateDirectory(Path.GetDirectoryName(path + "/data/" + ent.FullName));
                        Stream dO = File.OpenWrite(path + "/data/" + ent.FullName);
                        data.CopyTo(dO);
                        data.Close();
                        dO.Close();
                    }
                    archive.Dispose();
                }
                catch
                {
                    if (strm != null)
                        strm.Close();
                    try
                    {
                        Directory.Delete(path, true);
                    }
                    catch
                    {}
                    MessageBox.Show("An error occurred while trying to extract or run the installer, unable to continue!\n\nFailed to extract installer data! Please contact support for the program you are trying to install", "An error occurred", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Environment.Exit(1);
                    return;
                }

                try
                {
                    // Start launcher
                    Invoke(new Action(() =>
                    {
                        label1.Text = "Starting installer...";
                        progressBar1.Style = ProgressBarStyle.Marquee;
                    }));
                    string exe = "";
                    string args = "";
                    if (!File.Exists(path + "/data/startup.info"))
                    {
                        string cp = "";
                        foreach (FileInfo file in new DirectoryInfo(path + "/data/libs").GetFiles("*.jar"))
                        {
                            if (cp != "")
                                cp += ";";
                            cp += "\"libs/" + file.Name + "\"";
                        }
                        foreach (FileInfo file in new DirectoryInfo(path + "/data").GetFiles("*.jar"))
                        {
                            if (cp != "")
                                cp += ";";
                            cp += "\"" + file.Name + "\"";
                        }
                        exe = Path.GetFullPath(path + "/data/win/java-17/bin/javaw.exe");
                        args = "-cp " + cp + " org.asf.centuria.launcher.updater.LauncherUpdaterMain";
                    }
                    else
                    {
                        // Verify document
                        string[] lines = File.ReadAllText(path + "/data/startup.info").Replace("\r", "").Split('\n');
                        if (lines.Length == 0 || lines[0].Trim() == "")
                        {
                            if (strm != null)
                                strm.Close();
                            try
                            {
                                Directory.Delete(path, true);
                            }
                            catch
                            {}
                            MessageBox.Show("An error occurred while trying to extract or run the installer, unable to continue!\n\nCould not start the inner installer. The \"startup.info\" file is empty, expecting at least one line for the executable path and a optional second line for arguments.", "An error occurred", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            Environment.Exit(1);
                            return;
                        }
                        exe = Path.GetFullPath(path + "/data/" + lines[0]);
                        if (!File.Exists(exe))
                            exe = lines[0];
                        if (lines.Length >= 2 && lines[1].Trim() != "")
                            args = lines[1];
                    }
                    Process proc = new Process();
                    proc.StartInfo.UseShellExecute = false;
                    proc.StartInfo.RedirectStandardOutput = true;
                    proc.StartInfo.RedirectStandardError = true;
                    proc.StartInfo.FileName = exe;
                    proc.StartInfo.WorkingDirectory = Path.GetFullPath(path + "/data");
                    proc.StartInfo.Arguments = args + " " + Program.Arguments;
                    proc.Start();
                    Invoke(new Action(() => Hide()));
                    proc.WaitForExit();
                    Directory.Delete(path, true);
                    Environment.Exit(proc.ExitCode);
                }
                catch
                {
                    if (strm != null)
                        strm.Close();
                    try
                    {
                        Directory.Delete(path, true);
                    }
                    catch
                    {}
                    MessageBox.Show("An error occurred while trying to extract or run the installer, unable to continue!\n\nCould not start the inner installer. This could be due to file permissions or policy settings. Please contact support for the program you are trying to install", "An error occurred", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Environment.Exit(1);
                    return;
                }
            });
        }
    }
}
