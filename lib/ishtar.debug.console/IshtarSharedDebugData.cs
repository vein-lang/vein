using System.ComponentModel;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using ishtar;
using ishtar.emit;
using Newtonsoft.Json;
using Terminal.Gui;
using vein.runtime;
using static Unix.Terminal.Curses;
using Attribute = Terminal.Gui.Attribute;
using Rune = System.Rune;
using Window = Terminal.Gui.Window;

public static class IshtarSharedDebugData
{
    public static readonly object guarder = new object();
    

    private static IshtarState _state = new("UNK", "@entry", default, "NONE", 0);


    public static void SetState(IshtarState state)
    {
        if (state.GetHashCode() == _state.GetHashCode())
            return;

        lock (guarder)
        {
            _state = state;
            WriteItem($"IP: {_state.ip}, method: {_state.method}, cycle: {Math.Round(_state.cycleDelay.TotalMilliseconds, 3)}ms, lvl: {_state.invocationLevel}, sp: {_state.stackType}", stateView, false);
        }
    }


    public static void DumpToFile(FileInfo file, object o)
    {
        File.WriteAllText(file.FullName, JsonConvert.SerializeObject(o, Formatting.Indented));
    }

    
    public static void StdOutPush(string value)
    {
        lock (guarder)
        {
            WriteItem(value, stdOutView, false);
        }
    }

    public static void TraceOutPush(string value)
    {
        lock (guarder)
        {
            WriteItem(value, traceOutView, true);
        }
    }


    private static void WriteItem(string item, TextView view, bool insert)
    {
        var cts = new TaskCompletionSource();

        Application.MainLoop.Invoke(() =>
        {
            if (insert)
            {
                view.InsertText(item);
                view.InsertText("\n");
            }
            else
            {
                view.Text += item;
                view.Text += "\n";
            }
            view.MoveEnd();

            cts.SetResult();
        });
        Thread.Yield();
        cts.Task.Wait();
    }

   
    public static void Setup()
    {
        new Thread(_setup).Start();
        Thread.Sleep(200);
    }

    private static IshtarTraceTextView traceOutView;
    private static TextView stdOutView;
    private static TextView stateView;

    private static void _setup()
    {
        Application.Init();


        var customColors = new ColorScheme
        {
            Normal = Application.Driver.MakeColor(Color.White, Color.Black),
            Focus = Application.Driver.MakeColor(Color.Black, Color.Black),
            HotNormal = Application.Driver.MakeColor(Color.BrightYellow, Color.Black),
            HotFocus = Application.Driver.MakeColor(Color.Black, Color.Black),
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

        traceOutView = new IshtarTraceTextView()
        {
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            ColorScheme = customColors
        };

        traceOutView.Init();

        var mainTextViewFrame = new FrameView
        {
            Title = "Ishtar Trace",
            X = 0,
            Y = 0,
            Width = Dim.Percent(50),
            Height = Dim.Fill(),
            ColorScheme = customColors
        };
        mainTextViewFrame.Add(traceOutView);

        stdOutView = new TextView()
        {
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            ReadOnly = true,
            ColorScheme = customColors
        };

        var upperTextViewFrame = new FrameView()
        {
            Title = "StdOut",
            X = Pos.Percent(50) + 1,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Percent(50),
            CanFocus = false,
            ColorScheme = customColors
        };
        upperTextViewFrame.Add(stdOutView);

        stateView = new TextView()
        {
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            ReadOnly = true,
            ColorScheme = customColors
        };

        var tableViewFrame = new FrameView
        {
            Title = "Ishtar State",
            X = Pos.Percent(50) + 1,
            Y = Pos.Percent(50) + 1,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            CanFocus = false,
            ColorScheme = customColors
        };
        tableViewFrame.Add(stateView);

        win.Add(mainTextViewFrame, upperTextViewFrame, tableViewFrame);

        Application.Run();
    }




    private static string FormatState(IshtarState state) =>
        $"Instruction: {state.ip}\nMethod: {state.method}\nCycle: {state.cycleDelay.TotalMilliseconds - 1}ms\n" +
        $"StackType: {state.stackType}\nCallDepth: {state.invocationLevel}";
}

public class ConsoleBuffer(int maxSize)
{
    private readonly LinkedList<string> buffer = new();
    private int currentSize = 0;

    public void Add(string line)
    {
        int lines = line.Count(x => x == '\n');

        while (currentSize + lines > maxSize)
        {
            if (buffer.Count == 0)
                throw new InvalidOperationException("Cannot add item: buffer size exceeded and buffer is empty.");
            buffer.RemoveFirst();
            currentSize -= 1;
        }

        buffer.AddLast(line);
        currentSize += lines;
    }

    public string PrintBuffer() => string.Join('\n', buffer);
}

public record struct IshtarState
    (string ip, string method, TimeSpan cycleDelay, string stackType, int invocationLevel);

public class IshtarTraceTextView : TextView
{
    private HashSet<string> keywords = new HashSet<string>(StringComparer.CurrentCultureIgnoreCase);
    private HashSet<string> types = new HashSet<string>(StringComparer.CurrentCultureIgnoreCase);
    private Attribute cyan;
    private Attribute blue;
    private Attribute yellow;
    private Attribute white;
    private Attribute brow;

    public void Init()
    {
        keywords = OpCodes.all.Select(x => $"{x.Key}").ToHashSet();
        types = Enum.GetNames<VeinTypeCode>().ToHashSet();

        brow = Driver.MakeColor(Color.BrightRed, Color.Black);
        cyan = Driver.MakeColor(Color.Cyan, Color.Black);
        blue = Driver.MakeColor(Color.Blue, Color.Black);
        yellow = Driver.MakeColor(Color.BrightYellow, Color.Black);
        white = Driver.MakeColor(Color.White, Color.Black);
    }

    protected override void SetNormalColor() => Driver.SetAttribute(white);



    protected override void SetNormalColor(List<Rune> line, int idx)
    {
        if (IsInStringLiteral(line, idx))
            Driver.SetAttribute(brow);
        else if (IsHex(line, idx))
            Driver.SetAttribute(blue);
        else if (IsKeyword(line, idx))
            Driver.SetAttribute(cyan);
        else if (IsType(line, idx) || IsTypeName(line, idx))
            Driver.SetAttribute(yellow);
        else
            Driver.SetAttribute(white);
    }

    private bool IsInStringLiteral(List<Rune> line, int idx)
    {
        string strLine = new string (line.Select (r => (char)r).ToArray ());

        foreach (Match m in Regex.Matches(strLine, "'[^']*'"))
        {
            if (idx >= m.Index && idx < m.Index + m.Length)
                return true;
        }

        return false;
    }

    private bool IsHex(List<Rune> line, int idx)
    {
        string strLine = new string (line.Select (r => (char)r).ToArray ());

        foreach (Match m in Regex.Matches(strLine, "0x([A-F]|[0-9])+"))
        {
            if (idx >= m.Index && idx < m.Index + m.Length)
            {
                return true;
            }
        }

        return false;
    }

    private bool IsTypeName(List<System.Rune> line, int idx)
    {
        string strLine = new string (line.Select (r => (char)r).ToArray ());

        foreach (Match m in Regex.Matches(strLine, "[a-zA-Z0-9]+\\%[a-zA-Z0-9]+\\/[a-zA-Z0-9]+"))
        {
            if (idx >= m.Index && idx < m.Index + m.Length)
            {
                return true;
            }
        }

        return false;
    }

    private bool IsKeyword(List<System.Rune> line, int idx)
    {
        var word = IdxToWord (line, idx);

        if (string.IsNullOrWhiteSpace(word))
        {
            return false;
        }

        return keywords.Contains(word, StringComparer.InvariantCultureIgnoreCase);
    }

    private bool IsType(List<System.Rune> line, int idx)
    {
        var word = IdxToWord (line, idx);

        if (string.IsNullOrWhiteSpace(word))
        {
            return false;
        }

        return types.Contains(word, StringComparer.InvariantCultureIgnoreCase);
    }

    private string IdxToWord(List<System.Rune> line, int idx)
    {
        var words = Regex.Split (
            new string (line.Select (r => (char)r).ToArray ()),
            "\\b");


        int count = 0;
        string current = null;

        foreach (var word in words)
        {
            current = word;
            count += word.Length;
            if (count > idx)
            {
                break;
            }
        }

        return current?.Trim();
    }


}

public static class EventExtensions
{
    public static void ClearEventHandlers(this object obj, string eventName)
    {
        if (obj == null)
        {
            return;
        }

        Type objType = obj.GetType ();
        EventInfo eventInfo = objType.GetEvent (eventName);

        if (eventInfo == null)
        {
            return;
        }

        var isEventProperty = false;
        Type type = objType;
        FieldInfo eventFieldInfo = null;

        while (type != null)
        {
            /* Find events defined as field */
            eventFieldInfo = type.GetField(
                                            eventName,
                                            BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                                           );

            if (eventFieldInfo != null
                && (eventFieldInfo.FieldType == typeof(MulticastDelegate)
                    || eventFieldInfo.FieldType.IsSubclassOf(
                                                              typeof(MulticastDelegate)
                                                             )))
            {
                break;
            }

            /* Find events defined as property { add; remove; } */
            eventFieldInfo = type.GetField(
                                            "EVENT_" + eventName.ToUpper(),
                                            BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic
                                           );

            if (eventFieldInfo != null)
            {
                isEventProperty = true;

                break;
            }

            type = type.BaseType;
        }

        if (eventFieldInfo == null)
        {
            return;
        }

        if (isEventProperty)
        {
            // Default Events Collection Type
            RemoveHandler<EventHandlerList>(obj, eventFieldInfo);

            return;
        }

        if (!(eventFieldInfo.GetValue(obj) is Delegate eventDelegate))
        {
            return;
        }

        // Remove Field based event handlers
        foreach (Delegate d in eventDelegate.GetInvocationList())
        {
            eventInfo.RemoveEventHandler(obj, d);
        }
    }

    private static void RemoveHandler<T>(object obj, FieldInfo eventFieldInfo)
    {
        Type objType = obj.GetType ();
        object eventPropertyValue = eventFieldInfo.GetValue (obj);

        if (eventPropertyValue == null)
        {
            return;
        }

        PropertyInfo propertyInfo = objType.GetProperties (BindingFlags.NonPublic | BindingFlags.Instance)
                                           .FirstOrDefault (p => p.Name == "Events" && p.PropertyType == typeof (T));

        if (propertyInfo == null)
        {
            return;
        }

        object eventList = propertyInfo?.GetValue (obj, null);

        switch (eventList)
        {
            case null:
            return;
        }
    }
}
