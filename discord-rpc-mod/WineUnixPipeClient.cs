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
    // Modified version of the DiscordRPC ManagedNamedPipeClient for running in WINE
    public class WineUnixPipeClient : INamedPipeClient
    {
        public static class PipeBridge
        {
            [DllImport("winepipebridge", EntryPoint = "create_socket")]
            public static extern int CreateSocket();

            [DllImport("winepipebridge", EntryPoint = "connect_socket", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi)]
            public static extern int ConnectSocketInt(int sock, [MarshalAs(UnmanagedType.LPStr)] string path);

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

        private Stream _streamIn;
        private Stream _streamOut;
        private Process _pipeProc;

        private Queue<PipeFrame> _framequeue = new Queue<PipeFrame>();

        private object _framequeuelock = new object();

        private volatile bool _isDisposed;

        private volatile bool _isClosed = true;

        private object l_stream = new object();

        public ILogger Logger { get; set; }

        public bool IsConnected
        {
            get
            {
                if (_isClosed)
                {
                    return false;
                }
                lock (l_stream)
                {
                    return _pipeProc != null && !_pipeProc.HasExited;
                }
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
            if (_isDisposed)
            {
                throw new ObjectDisposedException("NamedPipe");
            }
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
                        Task.Run(() => ReadFrames());
                        return true;
                    }
                }
            }
            else if (AttemptConnection(pipe) || AttemptConnection(pipe, isSandbox: true))
            {
                Task.Run(() => ReadFrames());
                return true;
            }
            return false;
        }

        private bool AttemptConnection(int pipe, bool isSandbox = false)
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException("_stream");
            }
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
                lock (l_stream)
                {
                    Logger.Info("Attempting to connect to '{0}'", pipeName);

                    // Create socket
                    int sock = PipeBridge.CreateSocket();
                    if (sock < 0)
                        throw new IOException();

                    // Attempt connection
                    if (!PipeBridge.ConnectSocket(sock, pipeName))
                        throw new IOException();

                    // Send
                    MemoryStream strm = new MemoryStream();
                    strm.Write(new byte[] { 0, 0, 0, 0 });
                    strm.Write(new byte[] { 0x29, 0, 0, 0 });
                    strm.Write(Encoding.UTF8.GetBytes("{\"v\":1,\"client_id\":\"1115933633967050812\"}"));
                    byte[] data = strm.ToArray();
                    PipeBridge.SendToSocket(sock, data, data.Length, 0);
                    byte[] buf2 = new byte[2048];
                    int read = PipeBridge.ReadFromSocket(sock, buf2, buf2.Length, 0);

                    Process proc = new Process();
                    proc.StartInfo.FileName = FeralTweaks.FeralTweaksLoader.GetLoadedMod<RpcMod>().ModBaseDirectory + "/winepipebridge.exe";
                    proc.StartInfo.ArgumentList.Add(pipeName);
                    proc.StartInfo.UseShellExecute = false;
                    proc.StartInfo.RedirectStandardError = true;
                    proc.StartInfo.RedirectStandardOutput = true;
                    proc.StartInfo.RedirectStandardInput = true;
                    proc.StartInfo.CreateNoWindow = true;
                    proc.Start();

                    byte[] buf = new byte[2];
                    byte[] expected = Encoding.UTF8.GetBytes("OK");
                    proc.StandardOutput.BaseStream.Read(buf, 0, 2);
                    for (int i = 0; i < expected.Length; i++)
                        if (buf[i] != expected[i])
                        {
                            proc.Kill();
                            throw new IOException();
                        }
                    if (proc.HasExited)
                        throw new IOException();
                    _pipeProc = proc;
                    _streamOut = proc.StandardInput.BaseStream;
                    _streamIn = proc.StandardOutput.BaseStream;
                }
                Logger.Info("Connected to '{0}'", pipeName);
                _connectedPipe = pipe;
                _isClosed = false;
            }
            catch (Exception ex)
            {
                Logger.Error("Failed connection to {0}. {1}", pipeName, ex.Message);
                Close();
            }
            Logger.Trace("Done. Result: {0}", _isClosed);
            return !_isClosed;
        }

        private void ReadFrames()
        {
            if (_isClosed)
            {
                return;
            }
            try
            {
                while (IsConnected)
                {
                    Logger.Trace("Attempting to read frame...");
                    try
                    {
                        lock (l_stream)
                        {
                            if (_pipeProc != null && !_pipeProc.HasExited)
                            {
                                PipeFrame frame = new PipeFrame();
                                frame.ReadStream(_streamIn);
                                lock (_framequeuelock)
                                {
                                    _framequeue.Enqueue(frame);
                                }
                            }
                            else
                                break;
                        }
                    }
                    catch (IOException)
                    {
                        break;
                    }
                }
            }
            catch (ObjectDisposedException)
            {
                Logger.Warning("Attempted to start reading from a disposed pipe");
            }
            catch (InvalidOperationException)
            {
                Logger.Warning("Attempted to start reading from a closed pipe");
            }
            catch (Exception ex3)
            {
                Logger.Error("An exception occured while starting to read a stream: {0}", ex3.Message);
                Logger.Error(ex3.StackTrace);
            }
        }

        public bool ReadFrame(out PipeFrame frame)
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException("_stream");
            }
            lock (_framequeuelock)
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
            if (_isDisposed)
            {
                throw new ObjectDisposedException("_stream");
            }
            if (_isClosed || !IsConnected)
            {
                Logger.Error("Failed to write frame because the stream is closed");
                return false;
            }
            try
            {
                frame.WriteStream(_streamOut);
                return true;
            }
            catch (IOException ex)
            {
                Logger.Error("Failed to write frame because of a IO Exception: {0}", ex.Message);
            }
            catch (ObjectDisposedException)
            {
                Logger.Warning("Failed to write frame as the stream was already disposed");
            }
            catch (InvalidOperationException)
            {
                Logger.Warning("Failed to write frame because of a invalid operation");
            }
            return false;
        }

        public void Close()
        {
            if (_isClosed)
            {
                Logger.Warning("Tried to close a already closed pipe.");
                return;
            }
            try
            {
                lock (l_stream)
                {
                    if (_streamOut != null)
                    {
                        try
                        {
                            _streamOut.Flush();
                            _streamOut.Dispose();
                            _streamIn.Dispose();
                        }
                        catch (Exception)
                        {
                        }
                        try
                        {
                            _pipeProc.Kill();
                        }
                        catch { }
                        _pipeProc = null;
                        _streamIn = null;
                        _streamOut = null;
                        _isClosed = true;
                    }
                    else
                    {
                        Logger.Warning("Stream was closed, but no stream was available to begin with!");
                    }
                }
            }
            catch (ObjectDisposedException)
            {
                Logger.Warning("Tried to dispose already disposed stream");
            }
            finally
            {
                _isClosed = true;
                _connectedPipe = -1;
            }
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }
            if (!_isClosed)
            {
                Close();
            }
            lock (l_stream)
            {
                try
                {
                    _streamOut.Flush();
                    _streamOut.Dispose();
                    _streamIn.Dispose();
                }
                catch
                {
                }
                try
                {
                    _pipeProc.Kill();
                }
                catch { }
                _pipeProc = null;
                _streamIn = null;
                _streamOut = null;
                _isClosed = true;
            }
            _isDisposed = true;
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