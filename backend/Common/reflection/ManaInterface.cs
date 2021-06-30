namespace mana.runtime
{
    using System.Collections.Generic;

    public abstract class ManaInterface<T> : ManaClass where T : ManaInterface<T>
    {
        protected ManaInterface(QualityTypeName name, IEnumerable<T> parents, ManaModule module)
        {
            base.Owner = module;
            this.FullName = name;
            this.Parents.AddRange(parents);
        }

        public List<T> Parents { get; } = new();

        public sealed override bool IsInterface => true;

        protected sealed override ManaMethod GetOrCreateTor(string name, bool isStatic = false)
            => throw new InterfaceCantContainsCtorException();

        public sealed override ManaMethod GetDefaultCtor()
            => throw new InterfaceCantContainsCtorException();

        public sealed override ManaMethod GetDefaultDtor()
            => throw new InterfaceCantContainsCtorException();
    }
}
