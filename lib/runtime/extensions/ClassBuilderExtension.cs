namespace insomnia.emit
{
    internal static class ClassBuilderExtension
    {
        public static WaveType AsType(this ClassBuilder builder) 
            => new WaveTypeImpl(builder.GetName(), WaveTypeCode.TYPE_CLASS, builder.Flags);
    }
}