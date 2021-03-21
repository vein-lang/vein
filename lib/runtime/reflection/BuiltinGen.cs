namespace wave.emit
{
    using static MethodFlags;
    using static WaveTypeCode;

    internal static class BuiltinGen
    {
        public static WaveType GenerateConsole(WaveModuleBuilder module)
        {
            var clazz = module.DefineClass("global::wave/lang/console");

            clazz.DefineMethod("println", Extern | Static | Public, 
                TYPE_VOID.AsType(),
                ("value", TYPE_STRING));
            clazz.DefineMethod("print", Extern | Static | Public,
                TYPE_VOID.AsType(),
                ("value", TYPE_STRING));

            return clazz.AsType();
        }
    }
}