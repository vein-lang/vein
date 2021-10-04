namespace vein.runtime
{
    public abstract class ManaMember
    {
        public abstract string Name { get; protected set; }
        public abstract ManaMemberKind Kind { get; }
        public virtual bool IsSpecial { get; }
    }
}
