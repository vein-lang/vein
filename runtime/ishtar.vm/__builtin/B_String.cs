namespace ishtar
{
    using System;
    using static vein.runtime.MethodFlags;
    using static vein.runtime.VeinTypeCode;

    public static unsafe class B_String
    {
        public static void InitTable(Dictionary<string, RuntimeIshtarMethod> table)
        {
            new RuntimeIshtarMethod("i_call_String_Concat", Private | Static | Extern,
                    ("v1", TYPE_STRING), ("v2", TYPE_STRING))
                .AsNative((delegate*<CallFrame, IshtarObject**, IshtarObject*>)&Concat)
                .AddInto(table, x => x.Name);

            new RuntimeIshtarMethod("i_call_String_Equal", Private | Static | Extern,
                    ("v1", TYPE_STRING), ("v2", TYPE_STRING))
                .AsNative((delegate*<CallFrame, IshtarObject**, IshtarObject*>)&StrEqual)
                .AddInto(table, x => x.Name);

            new RuntimeIshtarMethod("i_call_String_Trim_Start", Private | Static | Extern,
                    ("v1", TYPE_STRING))
                .AsNative((delegate*<CallFrame, IshtarObject**, IshtarObject*>)&TrimStart)
                .AddInto(table, x => x.Name);

            new RuntimeIshtarMethod("i_call_String_Trim_End", Private | Static | Extern,
                    ("v1", TYPE_STRING))
                .AsNative((delegate*<CallFrame, IshtarObject**, IshtarObject*>)&TrimEnd)
                .AddInto(table, x => x.Name);

            new RuntimeIshtarMethod("i_call_String_fmt", Private | Static | Extern,
                    ("template", TYPE_STRING), ("array", TYPE_ARRAY))
                .AsNative((delegate*<CallFrame, IshtarObject**, IshtarObject*>)&Fmt)
                .AddInto(table, x => x.Name);

            new RuntimeIshtarMethod("i_call_String_Contains", Private | Static | Extern,
                    ("v1", TYPE_STRING), ("v2", TYPE_STRING))
                .AsNative((delegate*<CallFrame, IshtarObject**, IshtarObject*>)&Contains)
                .AddInto(table, x => x.Name);
        }

        [IshtarExportFlags(Private | Static)]
        [IshtarExport(2, "i_call_String_fmt")]
        public static IshtarObject* Fmt(CallFrame frame, IshtarObject** args)
        {
            var template_obj = args[0];
            var array_obj = args[1];

            FFI.StaticValidate(frame, &template_obj);
            FFI.StaticValidate(frame, &array_obj);

            FFI.StaticTypeOf(frame, &template_obj, TYPE_STRING);
            FFI.StaticTypeOf(frame, &array_obj, TYPE_ARRAY);


            var arr = (IshtarArray*)array_obj;

            var dotnet_arr = new string[arr->length];

            for (var i = 0ul; i != arr->length; i++)
            {
                dotnet_arr[i] = IshtarMarshal.ToDotnetString(arr->Get((uint)i, frame), frame);
            }

            var template = IshtarMarshal.ToDotnetString(template_obj, frame);

            var result = string.Format(template, dotnet_arr);

            return IshtarMarshal.ToIshtarObject(result, frame);

        }

        [IshtarExportFlags(Private | Static)]
        [IshtarExport(2, "i_call_String_Concat")]
        public static IshtarObject* Concat(CallFrame frame, IshtarObject** args)
        {
            var i_str1 = args[0];
            var i_str2 = args[1];

            FFI.StaticValidate(frame, &i_str1);
            FFI.StaticValidate(frame, &i_str2);
            FFI.StaticTypeOf(frame, &i_str1, TYPE_STRING);
            FFI.StaticTypeOf(frame, &i_str2, TYPE_STRING);


            var str1 = IshtarMarshal.ToDotnetString(i_str1, frame);
            var str2 = IshtarMarshal.ToDotnetString(i_str2, frame);

            var result = string.Concat(str1, str2);

            return IshtarMarshal.ToIshtarObject(result, frame);
        }


        [IshtarExportFlags(Private | Static)]
        [IshtarExport(2, "i_call_String_Contains")]
        public static IshtarObject* Contains(CallFrame frame, IshtarObject** args)
        {
            var i_str1 = args[0];
            var i_str2 = args[1];

            FFI.StaticValidate(frame, &i_str1);
            FFI.StaticValidate(frame, &i_str2);
            FFI.StaticTypeOf(frame, &i_str1, TYPE_STRING);
            FFI.StaticTypeOf(frame, &i_str2, TYPE_STRING);


            var str1 = IshtarMarshal.ToDotnetString(i_str1, frame);
            var str2 = IshtarMarshal.ToDotnetString(i_str2, frame);

            var result = str1.Contains(str2);

            return IshtarMarshal.ToIshtarObject(result, frame);
        }

        [IshtarExportFlags(Private | Static)]
        [IshtarExport(2, "i_call_String_Equal")]
        public static IshtarObject* StrEqual(CallFrame frame, IshtarObject** args)
        {
            var i_str1 = args[0];
            var i_str2 = args[1];

            FFI.StaticValidate(frame, &i_str1);
            FFI.StaticValidate(frame, &i_str2);
            FFI.StaticTypeOf(frame, &i_str1, TYPE_STRING);
            FFI.StaticTypeOf(frame, &i_str2, TYPE_STRING);

            var str1 = IshtarMarshal.ToDotnetString(i_str1, frame);
            var str2 = IshtarMarshal.ToDotnetString(i_str2, frame);

            var result = str1.Equals(str2);

            return IshtarMarshal.ToIshtarObject(result, frame);
        }

        public static IshtarObject* TemplateFunctionApply(CallFrame frame, IshtarObject** args, Func<string, string> apply)
        {
            var str1 = args[0];
            FFI.StaticValidate(frame, &str1);
            FFI.StaticTypeOf(frame, &str1, TYPE_STRING);

            var clr_str = IshtarMarshal.ToDotnetString(str1, frame);

            var result = apply(clr_str);

            return IshtarMarshal.ToIshtarObject(result, frame);
        }


        [IshtarExportFlags(Private | Static)]
        [IshtarExport(1, "i_call_String_trim_start")]
        public static IshtarObject* TrimStart(CallFrame frame, IshtarObject** args)
            => TemplateFunctionApply(frame, args, x => x.TrimStart());
        [IshtarExport(1, "i_call_String_trim_end")]
        public static IshtarObject* TrimEnd(CallFrame frame, IshtarObject** args)
            => TemplateFunctionApply(frame, args, x => x.TrimEnd());
    }
}
