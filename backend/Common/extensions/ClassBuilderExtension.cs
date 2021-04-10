namespace insomnia.emit
{
    internal static class ClassBuilderExtension
    {
        public static WaveType AsType(this ClassBuilder builder)
        {
            var t = new WaveTypeImpl(builder.GetName(), builder.TypeCode, builder.Flags);
            t.Members.AddRange(builder.Fields);
            t.Members.AddRange(builder.Methods);

            if (builder.Parent is not null)
                t.Parent = builder.Parent.AsType();
            return t;
        }
    }
}