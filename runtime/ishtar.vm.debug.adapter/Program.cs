namespace ishtar.debugger;



using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Ookii.CommandLine;

using static System.FormattableString;


internal class ProgramArgs
{
    [CommandLineArgument("debug", DefaultValue = false, IsRequired = false)]
    public bool Debug { get; set; }

    [CommandLineArgument("nodebug", DefaultValue = false, IsRequired = false)]
    public bool NoDebug { get; set; }

    [CommandLineArgument("server", DefaultValue = 0, IsRequired = false)]
    public int ServerPort { get; set; }

    [CommandLineArgument("stepOnEnter", DefaultValue = false, IsRequired = false)]
    public bool StepOnEnter { get; set; }
}

internal class Program
{
    private static void Main(string[] args)
    {
        CommandLineParser parser = new CommandLineParser(typeof(ProgramArgs));
        ProgramArgs arguments = null;

        try
        {
            arguments = (ProgramArgs)parser.Parse(args);
        }
        catch (CommandLineArgumentException ex)
        {
            Console.WriteLine(ex.Message);
            parser.WriteUsageToConsole();
            return;
        }

        if (arguments.Debug)
        {
            Console.WriteLine("Waiting for debugger...");
            while (true)
            {
                if (Debugger.IsAttached)
                {
                    Console.WriteLine("Debugger attached!");
                    break;
                }

                Thread.Sleep(100);
            }
        }

        if (arguments.StepOnEnter && arguments.ServerPort == 0)
        {
            Console.WriteLine("Continue-on-Enter requires server mode.");
            return;
        }

        if (arguments.ServerPort != 0)
        {
            // Server mode - listen on a network socket
            RunServer(arguments);
        }
        else
        {
            // Standard mode - run with the adapter connected to the process's stdin and stdout
            IshtarDebugAdapter adapter = new IshtarDebugAdapter(Console.OpenStandardInput(), Console.OpenStandardOutput());
            adapter.Protocol.LogMessage += (sender, e) => Debug.WriteLine(e.Message);
            adapter.Run();
        }
    }

    private static void RunServer(ProgramArgs args)
    {
        Console.WriteLine(Invariant($"Waiting for connections on port {args.ServerPort}..."));
        IshtarDebugAdapter adapter = null;

        Thread listenThread = new Thread(() =>
        {
            TcpListener listener = new TcpListener(IPAddress.Parse("127.0.0.1"), args.ServerPort);
            listener.Start();

            while (true)
            {
                Socket clientSocket = listener.AcceptSocket();
                Thread clientThread = new Thread(() =>
                {
                    Console.WriteLine("Accepted connection");

                    using (Stream stream = new NetworkStream(clientSocket))
                    {
                        adapter = new IshtarDebugAdapter(stream, stream);
                        adapter.Protocol.LogMessage += (sender, e) => Console.WriteLine(e.Message);
                        adapter.Protocol.DispatcherError += (sender, e) =>
                        {
                            Console.Error.WriteLine(e.Exception.Message);
                        };
                        adapter.Run();
                        adapter.Protocol.WaitForReader();

                        adapter = null;
                    }

                    Console.WriteLine("Connection closed");
                });

                clientThread.Name = "DebugServer connection thread";
                clientThread.Start();
            }
        });

        Thread keypressThread;
        if (args.StepOnEnter)
        {
            Console.WriteLine("Will step when ENTER is pressed.");
            keypressThread = new Thread(() =>
            {
                ConsoleKeyInfo keyInfo;

                while (true)
                {
                    keyInfo = Console.ReadKey();
                    if (keyInfo.Key == ConsoleKey.Enter && adapter != null)
                    {
                        Console.WriteLine("Forcing step");
                        adapter.ExitBreakCore(0, true);
                    }
                }
            });

            keypressThread.Name = "Keypress listener thread";
            keypressThread.Start();
        }

        listenThread.Name = "DebugServer listener thread";
        listenThread.Start();
        listenThread.Join();
    }
}
