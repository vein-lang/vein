namespace ishtar
{
    using System;
    using System.Runtime.CompilerServices;
    using static mana.runtime.MethodFlags;
    using static mana.runtime.ManaTypeCode;

    public static unsafe class B_String
    {
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern string FastAllocateString(int length);


        [IshtarExportFlags(Public | Static)]
        [IshtarExport(2, "@_fast_concat_string")]
        public static IshtarObject* FastConcat(CallFrame frame, IshtarObject** args)
        {
            var i_str1 = args[0];
            var i_str2 = args[1];

            FFI.StaticValidate(frame, &i_str1);
            FFI.StaticValidate(frame, &i_str2);


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


        [IshtarExport(1, "@_fast_trim_start_string")]
        public static IshtarObject* FastTrimStart(CallFrame frame, IshtarObject** args)
            => TemplateFunctionApply(frame, args, x => x.TrimStart());
        [IshtarExport(1, "@_fast_trim_end_string")]
        public static IshtarObject* FastTrimEnd(CallFrame frame, IshtarObject** args)
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
