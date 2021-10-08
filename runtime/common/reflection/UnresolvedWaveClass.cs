namespace vein.reflection
{
    using vein.runtime;

    public class UnresolvedVeinClass : VeinClass
    {
        public UnresolvedVeinClass(QualityTypeName fullName)
            => this.FullName = fullName;
    }
}
