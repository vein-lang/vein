namespace wave.extensions
{
    using runtime;

    internal static class ClassBuilderExtension
    {
        public static WaveType AsType(this WaveClass builder)
        {
            var t = new WaveTypeImpl(builder.FullName, builder.TypeCode, builder.Flags);
            t.Members.AddRange(builder.Fields);
            t.Members.AddRange(builder.Methods);

            if (builder.Parent is not null)
                t.Parent = builder.Parent.AsType();
            return t;
        }
    }
}