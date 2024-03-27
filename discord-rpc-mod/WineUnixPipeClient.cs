using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DiscordRPC;
using DiscordRPC.IO;
using DiscordRPC.Logging;

namespace FeralDiscordRpcMod
{
    public class WineUnixPipeClient : INamedPipeClient
    {
        public static class WineUtils
        {
            public static string GetHostSysName()
            {
                unsafe 
                {
                    char * sysName;
                    char * version;
                    wine_get_host_version(&sysName, &version);
                    string sysNameStr = Marshal.PtrToStringAnsi((IntPtr) sysName);
                    return sysNameStr;
                }
            }
            public static string GetHostVersion()
            {
                unsafe 
                {
                    char * sysName;
                    char * version;
                    wine_get_host_version(&sysName, &version);
                    string versionStr = Marshal.PtrToStringAnsi((IntPtr) version);
                    return versionStr;
                }
            }

            [DllImport("ntdll", EntryPoint = "wine_get_host_version")]
            public unsafe static extern string wine_get_host_version(char ** sysName, char ** version);
        }

        public static class PipeBridge
        {
            [DllImport("winepipebridge", EntryPoint = "create_socket")]
            public static extern int CreateSocket();

            [DllImport("winepipebridge", EntryPoint = "connect_socket", CallingConvention = CallingConvention.Cdecl)]
            public static extern int ConnectSocketInt(int sock, string path);

            [DllImport("winepipebridge", EntryPoint = "socket_shutdown")]
            public static extern void CloseSocket(int sock, int how);

            [DllImport("winepipebridge", EntryPoint = "socket_send")]
            public static extern int SendToSocket(int sock, byte[] data, int length, int flags);

            [DllImport("winepipebridge", EntryPoint = "socket_recv")]
            public static extern int ReadFromSocket(int sock, byte[] data, int length, int flags);

            public static void CloseSocket(int sock)
            {
                CloseSocket(sock, 2);
            }

            public static bool ConnectSocket(int sock, string path)
            {
                int i = ConnectSocketInt(sock, path);
                return i >= 0;
            }
        }

        private const string PIPE_NAME = "discord-ipc-{0}";

        private int _connectedPipe;
        private bool _connected;
        private int _pipeSock;

        private object wrLock = new object();
        private object rdLock = new object();
        private object frqLock = new object();

        public ILogger Logger { get; set; }

        public bool IsConnected
        {
            get
            {
                return _connected;
            }
        }

        public int ConnectedPipe => _connectedPipe;

        public WineUnixPipeClient()
        {
            Logger = new NullLogger();
        }

        public bool Connect(int pipe)
        {
            Logger.Trace("WineUnixPipeClient.Connection({0})", pipe);
            if (pipe > 9)
            {
                throw new ArgumentOutOfRangeException("pipe", "Argument cannot be greater than 9");
            }
            if (pipe < 0)
            {
                for (int i = 0; i < 10; i++)
                {
                    if (AttemptConnection(i) || AttemptConnection(i, isSandbox: true))
                    {
                        ReadFrames();
                        return true;
                    }
                }
            }
            else if (AttemptConnection(pipe) || AttemptConnection(pipe, isSandbox: true))
            {
                ReadFrames();
                return true;
            }
            return false;
        }

        private void ReadFrames()
        {
            Task.Run(() =>
            {
                while (_connected)
                {
                    try
                    {
                        PipeFrame fr = ReadFrame();
                        lock (frqLock)
                            _framequeue.Enqueue(fr);
                    }
                    catch
                    {
                        Close();
                        break;
                    }
                }
            });
        }

        private bool AttemptConnection(int pipe, bool isSandbox = false)
        {
            _connected = false;
            string text = (isSandbox ? GetPipeSandbox() : "");
            if (isSandbox && text == null)
            {
                Logger.Trace("Skipping sandbox connection.");
                return false;
            }
            Logger.Trace("Connection Attempt {0} ({1})", pipe, text);
            string pipeName = GetPipeName(pipe, text);
            try
            {
                Logger.Info("Attempting to connect to '{0}'", pipeName);

                // Create socket
                int sock = PipeBridge.CreateSocket();
                if (sock < 0)
                    throw new IOException();

                // Attempt connection
                if (!PipeBridge.ConnectSocket(sock, pipeName))
                    throw new IOException();

                // Success
                Logger.Info("Connected to '{0}'", pipeName);
                _connectedPipe = pipe;
                _pipeSock = sock;
                _connected = true;
            }
            catch (Exception ex)
            {
                Logger.Error("Failed connection to {0}. {1}", pipeName, ex.Message);
                Close();
            }
            Logger.Trace("Done. Result: {0}", _connected);
            return _connected;
        }

        private PipeFrame ReadFrame()
        {
            try
            {
                PipeFrame frame = new PipeFrame();
                lock (rdLock)
                {
                    // Read header
                    byte[] opcode = new byte[4];
                    int read = PipeBridge.ReadFromSocket(_pipeSock, opcode, opcode.Length, 0);
                    if (read <= 0)
                        throw new IOException("Stream was closed");
                    byte[] length = new byte[4];
                    read = PipeBridge.ReadFromSocket(_pipeSock, length, length.Length, 0);
                    if (read <= 0)
                        throw new IOException("Stream was closed");

                    // Parse header
                    frame.Opcode = (Opcode)BitConverter.ToUInt32(opcode, 0);
                    uint l = BitConverter.ToUInt32(length, 0);

                    // Read message
                    byte[] msg = new byte[l];
                    read = PipeBridge.ReadFromSocket(_pipeSock, msg, msg.Length, 0);
                    if (read <= -1)
                    {
                        throw new IOException("Stream was closed");
                    }

                    // Decode
                    frame.Message = frame.MessageEncoding.GetString(msg);
                }
                return frame;
            }
            catch (Exception ex)
            {
                Close();
                throw new IOException("Connection lost", ex);
            }
        }

        private Queue<PipeFrame> _framequeue = new Queue<PipeFrame>();
        public bool ReadFrame(out PipeFrame frame)
        {
            if (!_connected)
            {
                throw new ObjectDisposedException("_pipeSock");
            }
            lock (frqLock)
            {
                if (_framequeue.Count == 0)
                {
                    frame = default(PipeFrame);
                    return false;
                }
                frame = _framequeue.Dequeue();
                return true;
            }
        }

        public bool WriteFrame(PipeFrame frame)
        {
            if (!IsConnected)
            {
                Logger.Error("Failed to write frame because the stream is closed");
                return false;
            }
            try
            {
                lock (wrLock)
                {
                    MemoryStream strm = new MemoryStream();
                    frame.WriteStream(strm);
                    byte[] data = strm.ToArray();
                    PipeBridge.SendToSocket(_pipeSock, data, data.Length, 0);
                }
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to write frame because of an exception: {0}", ex.Message);
                Close();
            }
            return false;
        }

        public void Close()
        {
            if (!_connected)
                return;
            _connected = false;
            try
            {
                PipeBridge.CloseSocket(_pipeSock);
                _pipeSock = -1;
            }
            catch { }
            _pipeSock = -1;
            _connectedPipe = -1;
        }

        public void Dispose()
        {
            if (!_connected)
                return;
            _connected = false;
            try
            {
                PipeBridge.CloseSocket(_pipeSock);
                _pipeSock = -1;
            }
            catch { }
            _pipeSock = -1;
            _connectedPipe = -1;
        }

        public static string GetPipeName(int pipe, string sandbox)
        {
            return Path.Combine(GetTemporaryDirectory(), sandbox + $"discord-ipc-{pipe}").Replace(Path.DirectorySeparatorChar, '/');
        }

        public static string GetPipeName(int pipe)
        {
            return GetPipeName(pipe, "");
        }

        public static string GetPipeSandbox()
        {
            return "snap.discord/";
        }

        private static string GetTemporaryDirectory()
        {
            object obj = null ?? Environment.GetEnvironmentVariable("XDG_RUNTIME_DIR");
            if (obj == null)
            {
                obj = Environment.GetEnvironmentVariable("TMPDIR");
            }
            if (obj == null)
            {
                obj = Environment.GetEnvironmentVariable("TMP");
            }
            if (obj == null)
            {
                obj = Environment.GetEnvironmentVariable("TEMP");
            }
            if (obj == null)
            {
                obj = "/tmp";
            }
            return (string)obj;
        }
    }
}