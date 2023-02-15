using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using AssetRipper.VersionUtilities;
using FeralTweaks;
using FeralTweaksBootstrap.Detour;
using HarmonyLib.Public.Patching;
using Il2CppDumper;
using Il2CppInterop.Common;
using Il2CppInterop.Generator;
using Il2CppInterop.Generator.Runners;
using Il2CppInterop.HarmonySupport;
using Il2CppInterop.Runtime.Injection;
using Il2CppInterop.Runtime.InteropTypes;
using Il2CppInterop.Runtime.Startup;

namespace FeralTweaksBootstrap
{
    public static class Bootstrap
    {
        private const string VERSION = "v1.0.0-alpha-a1";
        private static Il2CppInteropRuntime runtime;
        private static RuntimeInvokeDetourContainer runtimeInvokeDetour;
        private static StreamWriter LogWriter;
        private static string GameAssemblyPath;

        public static void LogInfo(string message)
        {
            LogWriter.WriteLine("[" + DateTime.Now.ToString("HH:mm:ss:fff") + "] [INF] " + message);
        }

        public static void LogWarn(string message)
        {
            LogWriter.WriteLine("[" + DateTime.Now.ToString("HH:mm:ss:fff") + "] [WRN] " + message);
        }

        public static void LogError(string message)
        {
            LogWriter.WriteLine("[" + DateTime.Now.ToString("HH:mm:ss:fff") + "] [ERR] " + message);
        }

        public static void Start()
        {
            // Preprare
            Directory.CreateDirectory("FeralTweaks/cache");
            Directory.CreateDirectory("FeralTweaks/logs");

            // Set up log
            LogWriter = new StreamWriter("FeralTweaks/logs/preloader.log");
            LogWriter.AutoFlush = true;

            // Log
            LogInfo("Preparing...");
            LogInfo("FeralTweaks Bootstrapper version " + VERSION + " loading...");

            // Determine platform
            LogInfo("Determining platform...");
            PlatformType plat;
            if (File.Exists("GameAssembly.dll"))
                plat = PlatformType.WINDOWS;
            else if (File.Exists("Fer.al.app/Contents/Frameworks/GameAssembly.dylib"))
                plat = PlatformType.OSX;
            else
                plat = PlatformType.ANDROID;
            LogInfo("Platform: " + plat);

            // Build paths
            LogInfo("Resolving game paths...");
            string gameAssemblyPath = null;
            string dataPath = null;
            switch (plat)
            {
                case PlatformType.WINDOWS:
                    gameAssemblyPath = "GameAssembly.dll";
                    dataPath = "Fer.al_Data";
                    break;
                case PlatformType.OSX:
                    gameAssemblyPath = "Fer.al.app/Contents/Frameworks/GameAssembly.dylib";
                    dataPath = "Fer.al.app/Contents/Resources/Data";
                    break;
                case PlatformType.ANDROID:
                    // TODO: idfk how to do this atm
                    LogError("Android is presently non-functional");
                    Environment.Exit(1);
                    break;
            }
            string il2cppMetadata = dataPath + "/il2cpp_data/Metadata/global-metadata.dat";
            LogInfo("Data path: " + dataPath);
            LogInfo("Assembly path: " + gameAssemblyPath);
            LogInfo("IL2CPP metadata path: " + il2cppMetadata);
            GameAssemblyPath = gameAssemblyPath;

            // Log
            LogInfo("Determining versions...");

            // Determine unity version
            byte[] data = File.ReadAllBytes(dataPath + "/Resources/unity_builtin_extra");
            string buffer = "";
            for (int i = 48; i < data.Length; i++)
            {
                if (data[i] == 0)
                    break;
                else
                    buffer += (char)data[i];
            }
            string unityVer = buffer;
            LogInfo("Unity version: " + unityVer);

            // Generate assemblies
            if (!File.Exists("FeralTweaks/cache/assemblies/complete"))
            {
                LogInfo("Dumping dummy assemblies...");

                // Delete old
                if (Directory.Exists("FeralTweaks/cache/dummy"))
                {
                    try
                    {
                        Directory.Delete("FeralTweaks/cache/dummy", true);
                    }
                    catch
                    {
                        Directory.Delete("FeralTweaks/cache/dummy");
                    }
                }

                // Set up and dump into memory
                LogInfo("Loading metadata...");

                //
                // Run il2cppdumper
                // Credits to Prefare, adapted for internal use by Zera
                //

                // Load metadata
                MemoryStream strm = new MemoryStream(File.ReadAllBytes(il2cppMetadata));
                Metadata md = new Metadata(strm);

                // Read binary
                LogInfo("Loading Il2Cpp binary...");
                byte[] binBytes = File.ReadAllBytes(gameAssemblyPath);
                uint magic = BitConverter.ToUInt32(binBytes, 0);
                MemoryStream binStrm = new MemoryStream(binBytes);
                Il2Cpp bin;

                // Process
                LogInfo("Processing binary type...");
                switch(magic)
                {
                    // NSO
                    case 0x304F534E:
                        {
                            NSO nso = new NSO(binStrm);
                            bin = nso.UnCompress();
                            break;
                        }

                    // PE
                    case 0x905A4D:
                        {
                            bin = new PE(binStrm);
                            break;
                        }

                    // ELF
                    case 0x464c457f:
                        {
                            if (binBytes[4] == 2)
                            {
                                // ELF64
                                bin = new Elf64(binStrm);
                            }
                            else
                            {
                                // ELF32
                                bin = new Elf(binStrm);
                            }
                            break;
                        }

                    // Mach-O 64-bits
                    case 0xFEEDFACF:
                        {
                            bin = new Macho64(binStrm);
                            break;
                        }

                    // Mach-O 32-bits
                    case 0xFEEDFACE:
                        {
                            bin = new Macho(binStrm);
                            break;
                        }

                    // Fail
                    default:
                        {
                            LogError("Invalid game assembly file, cannot process " + gameAssemblyPath + " due to its type not being supported!");
                            Environment.Exit(1);
                            return;
                        }
                }

                // Setup
                LogInfo("Prepared metadata, loaded binary. IL2CPP Version: " + md.Version);
                LogInfo("Configuring...");
                bin.SetProperties(md.Version, md.metadataUsagesCount);

                // Check dump
                if (bin.CheckDump())
                {
                    // Check
                    if (bin is ElfBase)
                    {
                        // Fail
                        LogError("Unable to automatically select dump address, unable to continue!");
                        Environment.Exit(1);
                        return;
                    }
                    bin.IsDumped = true;
                }

                // Search
                LogInfo("Searching...");
                try
                {
                    var flag = bin.PlusSearch(md.methodDefs.Count(x => x.methodIndex >= 0), md.typeDefs.Length, md.imageDefs.Length);
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        if (!flag && bin is PE)
                        {
                            LogInfo("Use custom PE loader");
                            bin = PELoader.Load(gameAssemblyPath);
                            bin.SetProperties(md.Version, md.metadataUsagesCount);
                            flag = bin.PlusSearch(md.methodDefs.Count(x => x.methodIndex >= 0), md.typeDefs.Length, md.imageDefs.Length);
                        }
                    }
                    if (!flag)
                    {
                        flag = bin.Search();
                    }
                    if (!flag)
                    {
                        flag = bin.SymbolSearch();
                    }
                    if (!flag)
                    {
                        LogError("Can't use auto mode to process file, unable to continue!");
                        Environment.Exit(1);
                        return;
                    }
                    if (bin.Version >= 27 && bin.IsDumped)
                    {
                        var typeDef = md.typeDefs[0];
                        var il2CppType = bin.types[typeDef.byvalTypeIndex];
                        md.ImageBase = il2CppType.data.typeHandle - md.header.typeDefinitionsOffset;
                    }
                }
                catch
                {
                    LogError("An unexpected error occured, unable to continue!");
                    Environment.Exit(1);
                    return;
                }
                // Generate dlls
                LogInfo("Generating dummy dlls...");
                Il2CppExecutor exec = new Il2CppExecutor(md, bin);
                Directory.CreateDirectory("FeralTweaks/cache/dummy");
                DummyAssemblyGenerator dummy = new DummyAssemblyGenerator(exec, true);
                foreach (var assembly in dummy.Assemblies)
                {
                    LogInfo("Dumped " + assembly.MainModule.Name);
                    assembly.Write("FeralTweaks/cache/dummy/" + assembly.MainModule.Name);
                }
                LogInfo("Done!");

                // Clean up
                LogInfo("Cleaning...");
                strm.Close();
                binStrm.Close();

                // Delete old
                if (Directory.Exists("FeralTweaks/cache/assemblies"))
                {
                    try
                    {
                        Directory.Delete("FeralTweaks/cache/assemblies", true);
                    }
                    catch
                    {
                        Directory.Delete("FeralTweaks/cache/assemblies");
                    }
                }

                // Unhollow
                LogInfo("Unhollowing assemblies...");
                GeneratorOptions opts = new GeneratorOptions();
                opts.GameAssemblyPath = gameAssemblyPath;
                opts.Source = new AltDirCecilAssemblyResolver("FeralTweaks/cache/dummy").ToList();
                opts.OutputDir = "FeralTweaks/cache/assemblies";
                Il2CppInteropGenerator.Create(opts).AddLogger(new PreloaderLogger()).AddInteropAssemblyGenerator().Run();

                // Done
                File.Create("FeralTweaks/cache/assemblies/complete");
            }

            // Bind resolve
            LogInfo("Binding assembly resolution...");
            AppDomain.CurrentDomain.AssemblyResolve += (s, args) =>
            {
                // Attempt to resolve
                AssemblyName nm = new AssemblyName(args.Name);
                if (File.Exists("FeralTweaks/cache/assemblies/" + nm.Name + ".dll")){
                    return Assembly.LoadFile(Path.GetFullPath("FeralTweaks/cache/assemblies/" + nm.Name + ".dll"));
                }
                return null;
            };

            // Add to Cecil
            string trusted = (string)AppDomain.CurrentDomain.GetData("TRUSTED_PLATFORM_ASSEMBLIES");
            if (trusted == null)
                trusted = "";

            // Find assemblies
            foreach (FileInfo asm in new DirectoryInfo("CoreCLR").GetFiles("*.dll"))
                if (trusted == "")
                    trusted = asm.FullName;
                else
                    trusted += Path.PathSeparator + asm.FullName;

            // Set data
            AppDomain.CurrentDomain.SetData("TRUSTED_PLATFORM_ASSEMBLIES", trusted);

            // Set resolver
            LogInfo("Adding assembly resolver...");
            NativeLibrary.SetDllImportResolver(typeof(Il2CppInterop.Runtime.IL2CPP).Assembly, Resolver);

            // Create runtime
            LogInfo("Creating il2cpp interop runtime...");
            UnityVersion uver = UnityVersion.Parse(unityVer);
            runtime = Il2CppInteropRuntime.Create(new RuntimeConfiguration()
            {
                UnityVersion = new Version(uver.Major, uver.Minor, uver.Build),
                DetourProvider = new Il2CppDetourProvider()
            });

            // Load funchook for the current platform
            switch (plat)
            {
                case PlatformType.WINDOWS:
                    NativeLibrary.Load("FeralTweaks/lib/win/funchook.dll");
                    break;
                case PlatformType.OSX:
                    NativeLibrary.Load("libfunchook.dylib");
                    break;
                case PlatformType.ANDROID:
                    // TODO: idfk how to do this atm
                    LogError("Android is presently non-functional");
                    Environment.Exit(1);
                    break;
            }

            // Add harmony support
            LogInfo("Adding harmony support...");
            runtime = runtime.AddHarmonySupport();

            // Add logger
            LogInfo("Adding logger...");
            runtime = runtime.AddLogger(new InteropLogger());

            // Start runtime
            LogInfo("Starting runtime...");
            runtime.Start();

            // Hook
            IntPtr handle = NativeLibrary.Load(gameAssemblyPath, typeof(Il2CppInteropRuntime).Assembly, null);
            IntPtr invokePtr = NativeLibrary.GetExport(handle, "il2cpp_runtime_invoke");
            runtimeInvokeDetour = new RuntimeInvokeDetourContainer();
            NativeDetours.CreateDetour(invokePtr, runtimeInvokeDetour);

            // Start
            LogInfo("Preloader finished!");
            LogInfo("Starting FeralTweaks loader...");
            StartLoader();
        }

        private static IntPtr Resolver(string libraryName, Assembly assembly, DllImportSearchPath? searchPath)
        {
            if (libraryName == "GameAssembly")
                return NativeLibrary.Load(GameAssemblyPath, assembly, searchPath);
            return IntPtr.Zero;
        }

        private static void StartLoader()
        {
            FeralTweaksLoader.Start();
        }
    }

}