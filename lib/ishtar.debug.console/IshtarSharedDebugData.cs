using Terminal.Gui;

public static class IshtarSharedDebugData
{
    public static readonly object guarder = new object();

    public static Queue<string> StdOut { get; } = new();
    public static Queue<string> TraceOut { get; } = new();

    private static IshtarState _state = new("UNK", "@entry", default, "NONE", 0);


    public static void SetState(IshtarState state)
    {
        if (state.GetHashCode() == _state.GetHashCode())
            return;

        lock (guarder)
        {
            _state = state;
        }
    }

    public static IshtarState GetState()
    {
        lock (guarder)
        {
            return _state;
        }
    }


    public static bool StdOutHas()
    {
        lock (guarder)
        {
            return StdOut.Count != 0;
        }
    }
    public static bool TraceOutHas()
    {
        lock (guarder)
        {
            return TraceOut.Count != 0;
        }
    }

    public static string StdOutPop()
    {
        lock (guarder)
        {
            return StdOut.Dequeue();
        }
    }

    public static void StdOutPush(string value)
    {
        lock (guarder)
        {
            StdOut.Enqueue(value);
        }
    }

    public static string TraceOutPop()
    {
        lock (guarder)
        {
            return TraceOut.Dequeue();
        }
    }

    public static void TraceOutPush(string value)
    {
        lock (guarder)
        {
            TraceOut.Enqueue(value);
        }
    }

    public static void Setup()
    {
        new Thread(_setup).Start();
        Thread.Sleep(200);
    }
    private static void _setup()
    {
        Application.Init();


        var customColors = new ColorScheme
        {
            Normal = Application.Driver.MakeAttribute(Color.White, Color.Black),
            Focus = Application.Driver.MakeAttribute(Color.Black, Color.Black),
            HotNormal = Application.Driver.MakeAttribute(Color.BrightYellow, Color.Black),
            HotFocus = Application.Driver.MakeAttribute(Color.Black, Color.Black),
        };

        var top = Application.Top;

        var win = new Window
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            ColorScheme = customColors
        };

        win.Border = new Border()
        {
            BorderStyle = BorderStyle.None
        };

        top.Add(win);

        var traceOutView = new TextView()
        {
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            ReadOnly = true,
            ColorScheme = customColors
        };

        var mainTextViewFrame = new FrameView("Ishtar Trace")
        {
            X = 0,
            Y = 0,
            Width = Dim.Percent(50),
            Height = Dim.Fill(),
            CanFocus = false,
            ColorScheme = customColors
        };
        mainTextViewFrame.Add(traceOutView);

        var stdOutView = new TextView()
        {
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            ReadOnly = true,
            ColorScheme = customColors
        };

        var upperTextViewFrame = new FrameView("StdOut")
        {
            X = Pos.Percent(50) + 1,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Percent(50),
            CanFocus = false,
            ColorScheme = customColors
        };
        upperTextViewFrame.Add(stdOutView);

        var stateView = new TextView()
        {
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            ReadOnly = true,
            ColorScheme = customColors
        };

        var tableViewFrame = new FrameView("Ishtar State")
        {
            X = Pos.Percent(50) + 1,
            Y = Pos.Percent(50) + 1,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            CanFocus = false,
            ColorScheme = customColors
        };
        tableViewFrame.Add(stateView);

        win.Add(mainTextViewFrame, upperTextViewFrame, tableViewFrame);

        var updateThread = new Thread(() =>
        {
            while (true)
            {
                bool isSignalled = StdOutHas() || TraceOutHas();

                if (!isSignalled)
                {
                    Thread.Sleep(10);
                    continue;
                }


                ManualResetEvent @event = new ManualResetEvent(true);
                Application.MainLoop.Invoke(() =>
                {
                    while (StdOutHas())
                    {
                        var value = StdOutPop();
                        stdOutView.Text += value;
                        stdOutView.Text += "\n";
                        stdOutView.MoveEnd();
                    }

                    while (TraceOutHas())
                    {
                        var value = TraceOutPop();
                        traceOutView.Text += value;
                        traceOutView.Text += "\n";
                        traceOutView.MoveEnd();
                    }


                    stateView.Text = FormatState(GetState());

                    @event.Reset();
                });

                @event.WaitOne();
            }
        });

        updateThread.Start();

        Application.Run();
    }




    private static string FormatState(IshtarState state) =>
        $"Instruction: {state.ip}\nMethod: {state.method}\nCycle: {state.cycleDelay.TotalMilliseconds - 1}ms\n" +
        $"StackType: {state.stackType}\nCallDepth: {state.invocationLevel}";
}


public record IshtarState
    (string ip, string method, TimeSpan cycleDelay, string stackType, int invocationLevel);
