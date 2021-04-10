namespace wave.langserver
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.IO.Pipes;
    using System.Net.Sockets;
    using System.Threading.Tasks;
    using Serilog;
    internal class Program
    {
        public enum ReturnCode
        {
            SUCCESS = 0,
            MISSING_ARGUMENTS = 1,
            INVALID_ARGUMENTS = 2,
            MSBUILD_UNINITIALIZED = 3,
            CONNECTION_ERROR = 4,
            UNEXPECTED_ERROR = 100
        }

        private static int LogAndExit(ReturnCode code, string? message = null)
        {
            var text = message ?? (
                code == ReturnCode.SUCCESS ? "Exiting normally." :
                code == ReturnCode.MISSING_ARGUMENTS ? "Missing command line options." :
                code == ReturnCode.INVALID_ARGUMENTS ? "Invalid command line arguments. Use --help to see the list of options." :
                code == ReturnCode.MSBUILD_UNINITIALIZED ? "Failed to initialize MsBuild." :
                code == ReturnCode.CONNECTION_ERROR ? "Failed to connect." :
                code == ReturnCode.UNEXPECTED_ERROR ? "Exiting abnormally." : "");
            Log.Logger.Information(text);
            return (int)code;
        }
        
        public static async Task<int> Main(string[] args)
        {
            while (!Debugger.IsAttached) 
            {
                 await Task.Delay(100);
                 Console.WriteLine("Waiting debugger...");
            }
            
            Log.Logger = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .WriteTo.File("log.txt", rollingInterval: RollingInterval.Day)
                .MinimumLevel.Verbose()
                .CreateLogger();
            
            
            WaveLanguageServer server;
            try
            {
                server = /*ConnectViaNamedPipe("\\\\.\\pipe\\wave-lps", "\\\\.\\pipe\\wave-lps");*/ 
                    ConnectViaSocket();
            }
            catch (Exception ex)
            {
                Log.Logger.Error("[ERROR] Failed to launch server.");
                return LogAndExit(ReturnCode.CONNECTION_ERROR, ex.ToString());
            }

            Log.Logger.Information("Listening...");
            try
            {
                server.WaitForShutdown();
            }
            catch (Exception ex)
            {
                Log.Logger.Error("[ERROR] Unexpected error.");
                return LogAndExit(ReturnCode.UNEXPECTED_ERROR, ex.ToString());
            }

            return server.ReadyForExit
                ? LogAndExit(ReturnCode.SUCCESS)
                : LogAndExit(ReturnCode.UNEXPECTED_ERROR);
        }
        
        internal static WaveLanguageServer ConnectViaSocket(string hostname = "localhost", int port = 9092)
        {
            Log.Logger.Information($"Connecting via socket. {Environment.NewLine}Port number: {port}");
            Stream? stream = null;
            try
            {
                stream = new TcpClient(hostname, port).GetStream();
            }
            catch (Exception ex)
            {
                Log.Logger.Error("[ERROR] Failed to get network stream.");
                Log.Logger.Error(ex.ToString());
            }
            return new WaveLanguageServer(stream, stream);
        }
        
        internal static WaveLanguageServer ConnectViaNamedPipe(string writerName, string readerName)
        {
            Log.Logger.Information($"Connecting via named pipe. {Environment.NewLine}ReaderPipe: \"{readerName}\" {Environment.NewLine}WriterPipe:\"{writerName}\"");
            var writerPipe = new NamedPipeClientStream(writerName);
            var readerPipe = new NamedPipeClientStream(readerName);

            readerPipe.Connect(30000);
            if (!readerPipe.IsConnected)
            {
                Log.Logger.Error($"[ERROR] Connection attempted timed out.");
            }
            writerPipe.Connect(30000);
            if (!writerPipe.IsConnected)
            {
                Log.Logger.Error($"[ERROR] Connection attempted timed out.");
            }
            return new WaveLanguageServer(writerPipe, readerPipe);
        }
    }
}
