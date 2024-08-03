namespace vein.reflection
{
    using runtime;

    public record UnresolvedVeinClass : VeinClass
    {
        public UnresolvedVeinClass(QualityTypeName fullName)
            => FullName = fullName;
    }
}
