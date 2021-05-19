namespace mana
{
    using System;
    using System.Collections.Generic;
    using runtime;

    public interface IAssemblyResolver
    {
        ManaModule ResolveDep(string name, Version version, List<ManaModule> deps);
    }
}