namespace mana.reflection
{
    using runtime;

    public class UnresolvedManaClass : ManaClass
    {
        public UnresolvedManaClass(QualityTypeName fullName)
            => this.FullName = fullName;
    }
}
