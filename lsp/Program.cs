namespace mana.lsp
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.IO.Pipes;
    using System.Net.Sockets;
    using System.Reflection;
    using System.Runtime.Loader;
    using System.Threading.Tasks;
    using Serilog;
    using CommandLine;
    using CommandLine.Text;
    public class Program
    {
        public class Options
        {
            // Note: items in one set are mutually exclusive with items from other sets
            protected const string ConnectionViaSocket = "connectionViaSocket";
            protected const string ConnectionViaPipe = "connectionViaPipe";

            [Option(
                'p',
                "port",
                Required = true,
                SetName = ConnectionViaSocket,
                HelpText = "Port to use for TCP/IP connections.")]
            public int Port { get; set; }

            [Option(
                'w',
                "writer",
                Required = true,
                SetName = ConnectionViaPipe,
                HelpText = "Named pipe to write to.")]
            public string? WriterPipeName { get; set; }

            [Option(
                'r',
                "reader",
                Required = true,
                SetName = ConnectionViaPipe,
                HelpText = "Named pipe to read from.")]
            public string? ReaderPipeName { get; set; }
        }

        public enum ReturnCode
        {
            SUCCESS = 0,
            MISSING_ARGUMENTS = 1,
            INVALID_ARGUMENTS = 2,
            CONNECTION_ERROR = 4,
            UNEXPECTED_ERROR = 100,
        }

        public static string? Version { get; set; } =
            typeof(Program).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? typeof(Program).Assembly.GetName().Version?.ToString();

        private static int Main(string[] args)
        {
            //Debugger.Launch();
            //while (!Debugger.IsAttached)
            //{
            //    Task.Delay(100).Wait();
            //}
            Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);

            Log.Logger = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .WriteTo.File("log.txt", rollingInterval: RollingInterval.Day)
                .MinimumLevel.Verbose()
                .CreateLogger();

            var parser = new Parser(parser => parser.HelpWriter = null); // we want our own custom format for the version info
            var options = parser.ParseArguments<Options>(args);
            return options.MapResult(
                Run, errs => errs.IsVersion()
                    ? LogAndExit(ReturnCode.SUCCESS, message: Version)
                    : LogAndExit(ReturnCode.INVALID_ARGUMENTS, message: HelpText.AutoBuild(options)));

        }

        private static int LogAndExit(ReturnCode code, string? message = null)
        {
            var text = message ?? (
                code == ReturnCode.SUCCESS ? "Exiting normally." :
                code == ReturnCode.MISSING_ARGUMENTS ? "Missing command line options." :
                code == ReturnCode.INVALID_ARGUMENTS ? "Invalid command line arguments. Use --help to see the list of options." :
                code == ReturnCode.CONNECTION_ERROR ? "Failed to connect." :
                code == ReturnCode.UNEXPECTED_ERROR ? "Exiting abnormally." : "");
            Log.Logger.Error(text);
            return (int)code;
        }

        private static int Run(Options options)
        {
            if (options == null)
                return LogAndExit(ReturnCode.MISSING_ARGUMENTS);

            ManaLanguageServer server;
            try
            {
                server = options.ReaderPipeName != null && options.WriterPipeName != null
                    ? ConnectViaNamedPipe(options.WriterPipeName, options.ReaderPipeName)
                    : ConnectViaSocket(port: options.Port);
            }
            catch (Exception ex)
            {
                Log.Logger.Error("[ERROR] Failed to launch server.");
                return LogAndExit(ReturnCode.CONNECTION_ERROR, ex.ToString());
            }

            Log.Logger.Information("Listening...");
            try
            {
                //_ = server.CheckDotNetSdkVersionAsync();
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


        internal static ManaLanguageServer ConnectViaNamedPipe(string writerName, string readerName)
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

            return new ManaLanguageServer(writerPipe, readerPipe);
        }

        internal static ManaLanguageServer ConnectViaSocket(string hostname = "localhost", int port = 8008)
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

            return new ManaLanguageServer(stream, stream);
        }
    }
}
