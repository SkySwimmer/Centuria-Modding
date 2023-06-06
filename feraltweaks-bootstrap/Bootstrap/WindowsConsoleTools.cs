using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace FeralTweaksBootstrap
{
    internal static class WindowsConsoleTools
    {
        [DllImport("kernel32.dll")]
        private static extern IntPtr GetStdHandle(UInt32 nStdHandle);

        [DllImport("kernel32.dll")]
        private static extern void SetStdHandle(UInt32 nStdHandle, IntPtr handle);

        [DllImport("kernel32")]
        static extern bool AllocConsole();

        internal static void Attach()
        {
            // Open console
            AllocConsole();

            // Attach output
            TextWriter writer = new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true };
            Console.SetOut(writer);
            TextWriter wrErr = new StreamWriter(Console.OpenStandardError()) { AutoFlush = true };
            Console.SetError(wrErr);
        }
    }
}
