namespace mana.extensions
{
    using runtime;

    internal static class ClassBuilderExtension
    {
        public static ManaType AsType(this ManaClass builder)
        {
            var t = new ManaTypeImpl(builder.FullName, builder.TypeCode, builder.Flags);
            t.Members.AddRange(builder.Fields);
            t.Members.AddRange(builder.Methods);

            if (builder.Parent is not null)
                t.Parent = builder.Parent.AsType();
            return t;
        }
    }
}