namespace vein.runtime
{
    public abstract class VeinMember
    {
        public abstract string Name { get; protected set; }
        public abstract VeinMemberKind Kind { get; }
        public virtual bool IsSpecial { get; }
    }
}
