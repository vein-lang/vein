namespace ishtar
{
    using System;
    using System.Runtime.CompilerServices;
    using static vein.runtime.MethodFlags;
    using static vein.runtime.VeinTypeCode;

    public static unsafe class B_String
    {
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern string FastAllocateString(int length);


        public static void InitTable(Dictionary<string, RuntimeIshtarMethod> table)
        {
            new RuntimeIshtarMethod("i_call_String_Concat", Private | Static | Extern,
                    ("v1", TYPE_STRING), ("v2", TYPE_STRING))
                .AsNative((delegate*<CallFrame, IshtarObject**, IshtarObject*>)&Concat)
                .AddInto(table, x => x.Name);

            new RuntimeIshtarMethod("i_call_String_Trim_Start", Private | Static | Extern,
                    ("v1", TYPE_STRING))
                .AsNative((delegate*<CallFrame, IshtarObject**, IshtarObject*>)&TrimStart)
                .AddInto(table, x => x.Name);

            new RuntimeIshtarMethod("i_call_String_Trim_End", Private | Static | Extern,
                    ("v1", TYPE_STRING))
                .AsNative((delegate*<CallFrame, IshtarObject**, IshtarObject*>)&TrimEnd)
                .AddInto(table, x => x.Name);
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

        [IshtarExport(1, "@_fast_allocate_string")]
        [IshtarExportFlags(Private | Static)]
        public static IshtarObject* FastAllocateString(CallFrame frame, IshtarObject** args)
        {
            var len = args[0];

            FFI.StaticValidate(frame, &len);
            FFI.StaticTypeOf(frame, &len, TYPE_U4);
            var clr_len = (int)IshtarMarshal.ToDotnetUInt32(len, frame);

            var str = FastAllocateString(clr_len);
            return IshtarMarshal.ToIshtarObject(str, frame);
        }

        [IshtarExport(4, "@_fast_fill_string")]
        public static IshtarObject* FastFillString(CallFrame frame, IshtarObject** args)
        {
            var @ref = args[0];
            var @new = args[1];
            var offset = args[2];
            var len = args[3];


            FFI.StaticValidate(frame, &@ref); FFI.StaticValidate(frame, &@new);
            FFI.StaticValidate(frame, &offset); FFI.StaticValidate(frame, &len);

            FFI.StaticTypeOf(frame, &@ref, TYPE_STRING);
            FFI.StaticTypeOf(frame, &@new, TYPE_STRING);
            FFI.StaticTypeOf(frame, &offset, TYPE_U4);
            FFI.StaticTypeOf(frame, &len, TYPE_U4);

            var str_ref = IshtarMarshal.ToDotnetString(@ref, frame);
            var str_new = IshtarMarshal.ToDotnetString(@new, frame);
            var str_offset = IshtarMarshal.ToDotnetUInt32(offset, frame);
            var str_len = IshtarMarshal.ToDotnetUInt32(len, frame);

            return null;
        }
    }
}
