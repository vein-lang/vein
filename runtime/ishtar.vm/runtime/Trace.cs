namespace ishtar.vm.runtime;

using System.Text;
using ishtar.runtime;
using runtime;

[CTypeExport("ishtar_trace_t")]
internal readonly struct IshtarTrace()
{
    private readonly bool useConsole = Environment.GetCommandLineArgs().Contains("--sys::log::use-console=1");

    [Conditional("DEBUG")]
    public void Setup()
    {
        if (useConsole)
            return;
#if ISHTAR_DEBUG_CONSOLE
        IshtarSharedDebugData.Setup();
#endif
    }

    [Conditional("DEBUG")]
    public void println(string s)
    {
        if (useConsole)
        {
            Console.WriteLine(s);
            return;
        }
#if ISHTAR_DEBUG_CONSOLE
        IshtarSharedDebugData.TraceOutPush(s);
#endif
    }


    [Conditional("DEBUG")]
    public void debug_stdout_write(string s)
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

    [Conditional("DEBUG")]
    public unsafe void signal_state(OpCodeValue ip, CallFrame current, TimeSpan cycleDelay, stackval currentStack)
    {
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

    public void console_std_write(string s)
    {
#if DEBUG
        debug_stdout_write(s);
#else
        Console.WriteLine(s);
#endif
    }
}
