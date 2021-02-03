namespace wave.emit
{
    public static class WaveCore
    {
        public static readonly WaveClass ObjectClass;
        public static readonly WaveClass ValueTypeClass;
        public static readonly WaveClass VoidClass;
        public static readonly WaveClass StringClass;
        public static readonly WaveClass Int32Class;
        public static readonly WaveClass Int16Class;
        public static readonly WaveClass Int64Class;
        static WaveCore()
        {
            ObjectClass = new WaveClass("global::wave/lang/Object", null);
            ValueTypeClass = new WaveClass("global::wave/lang/ValueType", ObjectClass);
            VoidClass = new WaveClass("global::wave/lang/Void", ObjectClass);
            StringClass = new WaveClass("global::wave/lang/String", ObjectClass);
            Int16Class = new WaveClass("global::wave/lang/Int16", ValueTypeClass);
            Int32Class = new WaveClass("global::wave/lang/Int32", ValueTypeClass);
            Int64Class = new WaveClass("global::wave/lang/Int64", ValueTypeClass);
            
            
            ObjectClass.DefineMethod("getHashCode", Int32Class.AsType(WaveTypeCode.TYPE_I4), 
                MethodFlags.Virtual | MethodFlags.Public);
            ObjectClass.DefineMethod("toString", StringClass.AsType(WaveTypeCode.TYPE_STRING), 
                MethodFlags.Virtual | MethodFlags.Public);
        }
    }
}