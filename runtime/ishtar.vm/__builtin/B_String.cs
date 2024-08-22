namespace ishtar
{
    using Microsoft.VisualBasic;
    using static vein.runtime.MethodFlags;
    using static vein.runtime.VeinTypeCode;

    public static unsafe class B_String
    {
        private static IshtarObject* not_implemented(CallFrame* current, IshtarObject** args)
            => throw new NotImplementedException();
        private static IshtarObject* format_1(CallFrame* current, IshtarObject** args)
            => format(current, args[0], args + 1, 1);
        private static IshtarObject* format_2(CallFrame* current, IshtarObject** args)
            => format(current, args[0], args + 1, 2);
        private static IshtarObject* format_3(CallFrame* current, IshtarObject** args)
            => format(current, args[0], args + 1, 3);
        private static IshtarObject* format_4(CallFrame* current, IshtarObject** args)
            => format(current, args[0], args + 1, 4);
        private static IshtarObject* format_5(CallFrame* current, IshtarObject** args)
            => format(current, args[0], args + 1, 5);
        private static IshtarObject* format_6(CallFrame* current, IshtarObject** args)
            => format(current, args[0], args + 1, 6);

        private static IshtarObject* format(CallFrame* current, IshtarObject* template, IshtarObject** args, int size)
        {
            var template_str = template->_class->TypeCode is TYPE_STRING ?
                IshtarMarshal.ToDotnetString(template, current) :
                IshtarMarshal.ToDotnetString(IshtarMarshal.ToIshtarString(template, current), current);
            var strings = new string[size];

            for (int i = 0; i < size; i++) strings[i] = args[i]->_class->TypeCode is TYPE_STRING ?
                IshtarMarshal.ToDotnetString(args[i], current) :
                IshtarMarshal.ToDotnetString(IshtarMarshal.ToIshtarString(args[i], current), current);

            return current->vm->gc->ToIshtarObject(string.Format(template_str, strings), current);
        }

        private static IshtarObject* startsWith(CallFrame* current, IshtarObject** args)
        {
            var template_str = args[0]->_class->TypeCode is TYPE_STRING ?
                IshtarMarshal.ToDotnetString(args[0], current) :
                IshtarMarshal.ToDotnetString(IshtarMarshal.ToIshtarString(args[0], current), current);
            var target = args[0]->_class->TypeCode is TYPE_STRING ?
                IshtarMarshal.ToDotnetString(args[1], current) :
                IshtarMarshal.ToDotnetString(IshtarMarshal.ToIshtarString(args[1], current), current);

            return current->vm->gc->ToIshtarObject(template_str.StartsWith(target), current);
        }
        private static IshtarObject* endsWith(CallFrame* current, IshtarObject** args)
        {
            var template_str = args[0]->_class->TypeCode is TYPE_STRING ?
                IshtarMarshal.ToDotnetString(args[0], current) :
                IshtarMarshal.ToDotnetString(IshtarMarshal.ToIshtarString(args[0], current), current);
            var target = args[0]->_class->TypeCode is TYPE_STRING ?
                IshtarMarshal.ToDotnetString(args[1], current) :
                IshtarMarshal.ToDotnetString(IshtarMarshal.ToIshtarString(args[1], current), current);

            return current->vm->gc->ToIshtarObject(template_str.EndsWith(target), current);
        }

        public static void InitTable(ForeignFunctionInterface ffi)
        {
            ffi.Add("i_call_String_fmt([std]::std::String,[std]::std::Object) -> [std]::std::String",
                ffi.AsNative(&format_1));
            ffi.Add("i_call_String_fmt([std]::std::String,[std]::std::Object,[std]::std::Object) -> [std]::std::String",
                ffi.AsNative(&format_2));
            ffi.Add("i_call_String_fmt([std]::std::String,[std]::std::Object,[std]::std::Object,[std]::std::Object) -> [std]::std::String",
                ffi.AsNative(&format_3));
            ffi.Add("i_call_String_fmt([std]::std::String,[std]::std::Object,[std]::std::Object,[std]::std::Object,[std]::std::Object) -> [std]::std::String",
                ffi.AsNative(&format_4));
            ffi.Add("i_call_String_fmt([std]::std::String,[std]::std::Object,[std]::std::Object,[std]::std::Object,[std]::std::Object,[std]::std::Object) -> [std]::std::String",
                ffi.AsNative(&format_5));
            ffi.Add("i_call_String_fmt([std]::std::String,[std]::std::Object,[std]::std::Object,[std]::std::Object,[std]::std::Object,[std]::std::Object,[std]::std::Object) -> [std]::std::String",
                ffi.AsNative(&format_6));

            ffi.Add("i_call_String_StartsWith([std]::std::String,[std]::std::String) -> [std]::std::Boolean",
                ffi.AsNative(&startsWith));
            ffi.Add("i_call_String_EndsWith([std]::std::String,[std]::std::String) -> [std]::std::Boolean",
                ffi.AsNative(&endsWith));

            //method 'print_any([std]::std::Object) -> [std]::std::Void 

            ffi.Add("i_call_String_Concat", Private | Static | Extern, TYPE_STRING,
                    ("v1", TYPE_STRING), ("v2", TYPE_STRING))
                ->AsNative((delegate*<CallFrame*, IshtarObject**, IshtarObject*>)&Concat)
                ;

            ffi.Add("i_call_String_Equal", Private | Static | Extern, TYPE_BOOLEAN,
                    ("v1", TYPE_STRING), ("v2", TYPE_STRING))
                ->AsNative((delegate*<CallFrame*, IshtarObject**, IshtarObject*>)&StrEqual)
                ;

            ffi.Add("i_call_String_Trim_Start", Private | Static | Extern, TYPE_STRING,
                    ("v1", TYPE_STRING))
                ->AsNative((delegate*<CallFrame*, IshtarObject**, IshtarObject*>)&TrimStart)
                ;

            ffi.Add("i_call_String_Trim_End", Private | Static | Extern, TYPE_STRING,
                    ("v1", TYPE_STRING))
                ->AsNative((delegate*<CallFrame*, IshtarObject**, IshtarObject*>)&TrimEnd)
                ;

            ffi.Add("i_call_String_Contains", Private | Static | Extern, TYPE_BOOLEAN,
                    ("v1", TYPE_STRING), ("v2", TYPE_STRING))
                ->AsNative((delegate*<CallFrame*, IshtarObject**, IshtarObject*>)&Contains)
                ;
        }



        [IshtarExportFlags(Private | Static)]
        [IshtarExport(2, "i_call_String_fmt")]
        public static IshtarObject* Fmt(CallFrame* frame, IshtarObject** args)
        {
            var gc = frame->GetGC();
            var template_obj = args[0];
            var array_obj = args[1];

            ForeignFunctionInterface.StaticValidate(frame, &template_obj);
            ForeignFunctionInterface.StaticValidate(frame, &array_obj);

            ForeignFunctionInterface.StaticTypeOf(frame, &template_obj, TYPE_STRING);
            ForeignFunctionInterface.StaticTypeOf(frame, &array_obj, TYPE_ARRAY);


            var arr = (IshtarArray*)array_obj;

            var dotnet_arr = new string[arr->length];

            for (var i = 0ul; i != arr->length; i++)
            {
                dotnet_arr[i] = IshtarMarshal.ToDotnetString(arr->Get((uint)i, frame), frame);
            }

            var template = IshtarMarshal.ToDotnetString(template_obj, frame);

            var result = string.Format(template, dotnet_arr);

            return gc->ToIshtarObject(result, frame);

        }

        [IshtarExportFlags(Private | Static)]
        [IshtarExport(2, "i_call_String_Concat")]
        public static IshtarObject* Concat(CallFrame* frame, IshtarObject** args)
        {
            var gc = frame->GetGC();
            var i_str1 = args[0];
            var i_str2 = args[1];

            ForeignFunctionInterface.StaticValidate(frame, &i_str1);
            ForeignFunctionInterface.StaticValidate(frame, &i_str2);
            ForeignFunctionInterface.StaticTypeOf(frame, &i_str1, TYPE_STRING);
            ForeignFunctionInterface.StaticTypeOf(frame, &i_str2, TYPE_STRING);


            var str1 = IshtarMarshal.ToDotnetString(i_str1, frame);
            var str2 = IshtarMarshal.ToDotnetString(i_str2, frame);

            var result = string.Concat(str1, str2);

            return gc->ToIshtarObject(result, frame);
        }


        [IshtarExportFlags(Private | Static)]
        [IshtarExport(2, "i_call_String_Contains")]
        public static IshtarObject* Contains(CallFrame* frame, IshtarObject** args)
        {
            var gc = frame->GetGC();
            var i_str1 = args[0];
            var i_str2 = args[1];

            ForeignFunctionInterface.StaticValidate(frame, &i_str1);
            ForeignFunctionInterface.StaticValidate(frame, &i_str2);
            ForeignFunctionInterface.StaticTypeOf(frame, &i_str1, TYPE_STRING);
            ForeignFunctionInterface.StaticTypeOf(frame, &i_str2, TYPE_STRING);


            var str1 = IshtarMarshal.ToDotnetString(i_str1, frame);
            var str2 = IshtarMarshal.ToDotnetString(i_str2, frame);

            var result = str1.Contains(str2);

            return gc->ToIshtarObject(result, frame);
        }

        [IshtarExportFlags(Private | Static)]
        [IshtarExport(2, "i_call_String_Equal")]
        public static IshtarObject* StrEqual(CallFrame* frame, IshtarObject** args)
        {
            var gc = frame->GetGC();
            var i_str1 = args[0];
            var i_str2 = args[1];

            ForeignFunctionInterface.StaticValidate(frame, &i_str1);
            ForeignFunctionInterface.StaticValidate(frame, &i_str2);
            ForeignFunctionInterface.StaticTypeOf(frame, &i_str1, TYPE_STRING);
            ForeignFunctionInterface.StaticTypeOf(frame, &i_str2, TYPE_STRING);

            var str1 = IshtarMarshal.ToDotnetString(i_str1, frame);
            var str2 = IshtarMarshal.ToDotnetString(i_str2, frame);

            var result = str1.Equals(str2);

            return gc->ToIshtarObject(result, frame);
        }

        public static IshtarObject* TemplateFunctionApply(CallFrame* frame, IshtarObject** args, Func<string, string> apply)
        {
            var gc = frame->GetGC();
            var str1 = args[0];
            ForeignFunctionInterface.StaticValidate(frame, &str1);
            ForeignFunctionInterface.StaticTypeOf(frame, &str1, TYPE_STRING);

            var clr_str = IshtarMarshal.ToDotnetString(str1, frame);

            var result = apply(clr_str);

            return gc->ToIshtarObject(result, frame);
        }


        [IshtarExportFlags(Private | Static)]
        [IshtarExport(1, "i_call_String_trim_start")]
        public static IshtarObject* TrimStart(CallFrame* frame, IshtarObject** args)
            => TemplateFunctionApply(frame, args, x => x.TrimStart());
        [IshtarExport(1, "i_call_String_trim_end")]
        public static IshtarObject* TrimEnd(CallFrame* frame, IshtarObject** args)
            => TemplateFunctionApply(frame, args, x => x.TrimEnd());
    }
}
