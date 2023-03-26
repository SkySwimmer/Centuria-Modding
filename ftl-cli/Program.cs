using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Reflection;

namespace ftl_cli
{
    class Program
    {
        private static string FTL_DOWNLOAD_URL_OSX = "https://emuferal.ddns.net/ftl-osx-latest.zip";
        private static string FTL_DOWNLOAD_URL_WIN64 = "https://emuferal.ddns.net/ftl-win64-latest.zip";
        static void Main(string[] args)
        {
            // Check if feraltweaks is present
            if (!Directory.Exists("FeralTweaks"))
            {
                // Check for a game
                string game = null;
                bool osx = false;
                
                // Windows
                foreach (FileInfo file in new DirectoryInfo(".").GetFiles("*.exe"))
                {
                    string fName = Path.GetFileNameWithoutExtension(file.FullName);

                    // Check IL2CPP
                    if (File.Exists(fName + "_Data/il2cpp_data/Metadata/global-metadata.dat"))
                    {
                        game = fName;
                        if (fName == "Fer.al")
                            break; // Found a known compatible game
                        // If its not Fer.al, keep going
                    }
                }

                // MacOS
                foreach (DirectoryInfo dir in new DirectoryInfo(".").GetDirectories("*.app"))
                {
                    string fName = Path.GetFileNameWithoutExtension(dir.FullName);

                    // Check IL2CPP
                    if (File.Exists(dir.FullName + "/Contents/Frameworks/GameAssembly.dylib") && Directory.Exists(dir.FullName + "/Contents/Resources/Data"))
                    {
                        osx = true;
                        game = fName;
                        if (fName == "Fer.al")
                            break; // Found a known compatible game
                        // If its not Fer.al, keep going
                    }
                }

                // Check result
                if (game == null)
                {
                    // Error
                    Console.Error.WriteLine("Error: no FTL loader and no compatible Unity game found in the current directory.");
                    Environment.Exit(1);
                }
                else
                {
                    // Ask if they want to install the loader
                    Console.Error.WriteLine("Error: no FTL loader found in the current directory.");
                    Console.Write("An il2cpp Unity game was detected, do you want to download and set up the FTL modloader?");
                    if (game != "Fer.al")
                    {
                        // Warn
                        Console.WriteLine();
                        Console.WriteLine();
                        Console.WriteLine("WARNING! '" + game + "' is not a known compatible game! Please note that FTL is not designed as a universal modloader!");
                        Console.WriteLine("FTL is designed to work specifically for 64-bit games and targeting specifically a game known as Fer.al.");
                        Console.WriteLine("Please note that there is a HUGE chance for FTL to break while targeting " + game + "!");
                        Console.WriteLine();
                        Console.Write("Download and configure FTL?");
                    }
                    Console.Write(" [Y/n] ");
                    if (Console.ReadLine().ToLower() != "y")
                        Environment.Exit(1);

                    // Download FTL
                    string download = osx ? FTL_DOWNLOAD_URL_OSX : FTL_DOWNLOAD_URL_WIN64;
                    Console.WriteLine("Downloading latest FTL version from " + download + "...");

                    // Download to temporary file
                    FileStream outp = File.OpenWrite("ftl.zip");
                    HttpClient cl = new HttpClient();
                    Stream strm = cl.GetAsync(download).GetAwaiter().GetResult().Content.ReadAsStream();
                    strm.CopyTo(outp);
                    strm.Close();
                    outp.Close();

                    // Extract
                    Console.WriteLine("Extracting FTL...");
                    ZipArchive archive = ZipFile.OpenRead("ftl.zip");
                    foreach (ZipArchiveEntry ent in archive.Entries)
                    {
                        string entName = ent.FullName.Replace("\\", "/");
                        while (entName.StartsWith("/"))
                            entName = entName.Substring(1);
                        if (entName == "")
                            continue;
                        if (entName.EndsWith("/"))
                        {
                            // Directory
                            Directory.CreateDirectory(entName);
                        }
                        else
                        {
                            // File
                            Directory.CreateDirectory(Path.GetDirectoryName("./" + entName));
                            FileStream outFp = File.OpenWrite(entName);
                            Stream inFp = ent.Open();
                            inFp.CopyTo(outFp);
                            inFp.Close();
                            outFp.Close();
                        }
                        Console.WriteLine("Extracted " + ent.FullName);
                    }
                    archive.Dispose();
                    Console.WriteLine("Done.");
                    File.WriteAllText("game.info", game);
                    Console.WriteLine("");
                }
            }

            // Add assemblies to assembly resolution
            AppDomain.CurrentDomain.AssemblyResolve += (s, args) =>
            {
                // Attempt to resolve
                AssemblyName nm = new AssemblyName(args.Name);

                // Find file
                if (File.Exists("FeralTweaks/" + nm.Name + ".dll"))
                    return Assembly.LoadFile(Path.GetFullPath("FeralTweaks/" + nm.Name + ".dll"));

                // Not found
                return null;
            };

            // Run FTL
            Run();
        }

        private static void Run()
        {            
            // Run
            Console.WriteLine("FeralTweaksLoader Command Line Wrapper");
            Console.WriteLine("Copyright(c) AerialWorks Technologies, licensed GPL-2.0");
            Console.WriteLine("Use `ftl --help` for a list of arguments.");
            Console.WriteLine();
            Console.WriteLine("Please be aware that doing anything other than dry runs has a huge chance of causing errors.");
            Console.WriteLine("This tool does NOT start the actual game.");
            Console.WriteLine();
            FeralTweaksBootstrap.Bootstrap.Start();
            FeralTweaksBootstrap.Bootstrap.LogInfo("FTL exited, running through the CLI wrapper, not starting the game!");
        }
    }
}
