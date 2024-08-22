namespace ishtar.runtime;

using System.Diagnostics;
using System.Text;

[CTypeExport("ishtar_trace_t")]
internal readonly unsafe struct IshtarTrace(VirtualMachine* vm)
{
    private bool useConsole => vm->Config.UseConsole;
    private bool NoTrace => vm->Config.NoTrace;

    public void Setup()
    {
        if (useConsole)
            return;
#if ISHTAR_DEBUG_CONSOLE
        IshtarSharedDebugData.Setup();
#endif
    }
    public void log(string s)
    {
        if (useConsole)
        {
            if (NoTrace) return;
            Console.WriteLine(s);
            return;
        }
#if ISHTAR_DEBUG_CONSOLE
        IshtarSharedDebugData.TraceOutPush(s);
#endif
    }

    public void error(string s)
    {
        if (useConsole)
        {
            if (NoTrace) return;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(s);
            Console.ResetColor();
            return;
        }
#if ISHTAR_DEBUG_CONSOLE
        IshtarSharedDebugData.TraceOutPush(s);
#endif
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void debug_stdout_write_line(string s)
    {
        if (useConsole)
        {
            Console.WriteLine(s);
            return;
        }
#if ISHTAR_DEBUG_CONSOLE
        IshtarSharedDebugData.StdOutPush(s);
#endif
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void debug_stdout_write(string s)
    {
        if (useConsole)
        {
            Console.Write(s);
            return;
        }
#if ISHTAR_DEBUG_CONSOLE
        IshtarSharedDebugData.StdOutPush(s);
#endif
    }

    [Conditional("DEBUG")]
    public unsafe void signal_state(OpCodeValue ip, CallFrame current, TimeSpan cycleDelay, stackval currentStack)
    {
        if (NoTrace) return;
#if ISHTAR_DEBUG_CONSOLE
        IshtarSharedDebugData.SetState(new IshtarState($"{ip}", current.method->Name, cycleDelay,
            $"{currentStack.type}", current.level));
#endif
    }

    public static unsafe object Dump(RuntimeIshtarModule* module)
    {
        var classes = new Dictionary<string, object>();

        module->class_table->ForEach(x =>
        {
            classes.Add(x->Name, Dump(x));
        });

        return new
        {
            Name = module->Name,
            Version = module->Version,
            classes
        };
    }

    public static unsafe object Dump(RuntimeIshtarClass* clazz)
    {
        var methods = new Dictionary<string, object>();
        var fields = new Dictionary<string, object>();
        var aspects = new Dictionary<string, object>();

        var parentName = clazz->Parent == null ? null : clazz->Parent->Name;

        clazz->Methods->ForEach(x =>
        {
            methods.Add(x->Name, Dump(x));
        });


        return new
        {
            Name = clazz->Name,
            FullName = clazz->FullName->ToString(),
            Flags = clazz->Flags,
            ID = clazz->ID,
            Parent = parentName,
            methods,
            fields,
            aspects
        };
    }

    public static unsafe object Dump(RuntimeIshtarMethod* method)
    {
        string readBytes(uint* code, uint codeSize)
        {
            var strBuilder = new StringBuilder();
            var current = 0;
            while (current < codeSize)
            {
                strBuilder.Append($"0x{*code + current:X} ");
                current++;
            }
            return strBuilder.ToString();
        }

        return new
        {
            Name = method->Name,
            Flags = method->Flags,
            ReturnType = new
            {
                TypeCode = method->ReturnType->TypeCode,
                Name = method->ReturnType->Name,
                ID = method->ReturnType->ID
            },
            Header = new
            {
                method->PIInfo.compiled_func_ref,
                extern_function_declaration = method->PIInfo.extern_function_declaration.Name,
                jitted_wrapper = method->PIInfo.jitted_wrapper.Name,
                code_size = method->Header == null ? 0 : method->Header->code_size,
                body = method->Header == null ? "empty" : readBytes(method->Header->code, method->Header->code_size)
            }
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void console_std_write_line(string s) => debug_stdout_write_line(s);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void console_std_write(string s) => debug_stdout_write(s);
}
