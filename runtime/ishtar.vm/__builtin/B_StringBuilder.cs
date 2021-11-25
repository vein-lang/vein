namespace ishtar;

using System.Runtime.InteropServices;
using System.Text;
using vein.collections;
using vein.runtime;
using static vein.runtime.MethodFlags;

public unsafe static class B_StringBuilder
{
    [IshtarExport(2, "i_call_StringBuilder_append")]
    [IshtarExportFlags(Public | Static)]
    public static IshtarObject* Append(CallFrame current, IshtarObject** args)
    {
        var arg1 = args[0];
        var arg2 = args[1];

        var @class_1 = arg1->decodeClass();
        var @class_2 = arg2->decodeClass();

        FFI.StaticValidate(current, &arg1);

        var buffer = (ImmortalObject<StringBuilder>*)arg1->vtable[@class_1.Field["!!buffer"].vtable_offset];

        buffer->Value.AppendLine(IshtarMarshal.ToDotnetString(IshtarMarshal.ToIshtarString(arg2, current), current));

        return arg1;
    }
    [IshtarExport(2, "i_call_StringBuilder_appendLine")]
    [IshtarExportFlags(Public | Static)]
    public static IshtarObject* AppendLine(CallFrame current, IshtarObject** args)
    {
        var arg1 = args[0];
        var arg2 = args[1];

        
        var @class_1 = arg1->decodeClass();
        var @class_2 = arg2->decodeClass();

        FFI.StaticValidate(current, &arg1);
        FFI.StaticValidate(current, &arg2);
        
        var buffer = (ImmortalObject<StringBuilder>*)arg1->vtable[@class_1.Field["!!buffer"].vtable_offset];

        buffer->Value.AppendLine(IshtarMarshal.ToDotnetString(arg2, current));

        return arg1;
    }

    [IshtarExport(1, "i_call_StringBuilder_init_buffer")]
    [IshtarExportFlags(Public | Static)]
    public static IshtarObject* InitBuffer(CallFrame current, IshtarObject** args)
    {
        var arg1 = args[0];
        var @class_1 = arg1->decodeClass();
        FFI.StaticValidate(current, &arg1);
        arg1->vtable[@class_1.Field["!!buffer"].vtable_offset] =
            IshtarGC.AllocImmortal<StringBuilder>();
        return null;
    }

    [IshtarExport(1, "i_call_StringBuilder_clear_buffer")]
    [IshtarExportFlags(Public | Static)]
    public static IshtarObject* ClearBuffer(CallFrame current, IshtarObject** args)
    {
        var arg1 = args[0];
        var @class_1 = arg1->decodeClass();
        FFI.StaticValidate(current, &arg1);
        var buffer = (ImmortalObject<StringBuilder>*)arg1->vtable[@class_1.Field["!!buffer"].vtable_offset];
        buffer->Value.Clear();
        IshtarGC.FreeImmortal(buffer);
        return null;
    }

    [IshtarExport(1, "i_call_StringBuilder_toString")]
    [IshtarExportFlags(Public | Static)]
    public static IshtarObject* ToString(CallFrame current, IshtarObject** args)
    {
        var arg1 = args[0];
        var @class_1 = arg1->decodeClass();
        FFI.StaticValidate(current, &arg1);
        var buffer = (ImmortalObject<StringBuilder>*)arg1->vtable[@class_1.Field["!!buffer"].vtable_offset];
        return IshtarMarshal.ToIshtarObject(buffer->Value.ToString(), current);
    }

    internal static IshtarMetaClass ThisClass => IshtarMetaClass.Define("vein/lang", "StringBuilder");

    public static void InitTable(Dictionary<string, RuntimeIshtarMethod> table)
    {
        new RuntimeIshtarMethod("i_call_StringBuilder_append", Public | Static | Extern,
                new VeinArgumentRef("_this_", ThisClass), new VeinArgumentRef("value", VeinCore.ObjectClass))
            .AsNative((delegate*<CallFrame, IshtarObject**, IshtarObject*>)&Append)
            .AddInto(table, x => x.Name);
        new RuntimeIshtarMethod("i_call_StringBuilder_append", Public | Static | Extern,
                new VeinArgumentRef("_this_", ThisClass), new VeinArgumentRef("value", VeinCore.ValueTypeClass))
            .AsNative((delegate*<CallFrame, IshtarObject**, IshtarObject*>)&Append)
            .AddInto(table, x => x.Name);

        new RuntimeIshtarMethod("i_call_StringBuilder_appendLine", Public | Static | Extern,
                new VeinArgumentRef("_this_", ThisClass), new VeinArgumentRef("value", VeinCore.ObjectClass))
            .AsNative((delegate*<CallFrame, IshtarObject**, IshtarObject*>)&AppendLine)
            .AddInto(table, x => x.Name);
        new RuntimeIshtarMethod("i_call_StringBuilder_appendLine", Public | Static | Extern,
                new VeinArgumentRef("_this_", ThisClass), new VeinArgumentRef("value", VeinCore.ValueTypeClass))
            .AsNative((delegate*<CallFrame, IshtarObject**, IshtarObject*>)&AppendLine)
            .AddInto(table, x => x.Name);

        new RuntimeIshtarMethod("i_call_StringBuilder_init_buffer", Public | Static | Extern,
                new VeinArgumentRef("_this_", ThisClass))
            .AsNative((delegate*<CallFrame, IshtarObject**, IshtarObject*>)&InitBuffer)
            .AddInto(table, x => x.Name);

        new RuntimeIshtarMethod("i_call_StringBuilder_clear_buffer", Public | Static | Extern,
                new VeinArgumentRef("_this_", ThisClass))
            .AsNative((delegate*<CallFrame, IshtarObject**, IshtarObject*>)&ClearBuffer)
            .AddInto(table, x => x.Name);

        new RuntimeIshtarMethod("i_call_StringBuilder_toString", Public | Static | Extern,
                new VeinArgumentRef("_this_", ThisClass))
            .AsNative((delegate*<CallFrame, IshtarObject**, IshtarObject*>)&ToString)
            .AddInto(table, x => x.Name);
    }
}
