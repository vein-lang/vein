namespace vein.reflection
{
    using runtime;

    public class UnresolvedVeinClass : VeinClass
    {
        public UnresolvedVeinClass(QualityTypeName fullName)
            => FullName = fullName;
    }
}
