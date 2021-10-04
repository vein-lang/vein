namespace vein.reflection
{
    using vein.runtime;

    public class UnresolvedManaClass : ManaClass
    {
        public UnresolvedManaClass(QualityTypeName fullName)
            => this.FullName = fullName;
    }
}
