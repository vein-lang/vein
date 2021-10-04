namespace vein
{
    using System;
    using System.Collections.Generic;
    using vein.runtime;

    public interface IAssemblyResolver
    {
        ManaModule ResolveDep(string name, Version version, List<ManaModule> deps);
    }
}
