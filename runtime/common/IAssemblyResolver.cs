namespace vein
{
    using System;
    using System.Collections.Generic;
    using vein.runtime;

    public interface IAssemblyResolver
    {
        VeinModule ResolveDep(string name, Version version, List<VeinModule> deps);
    }
}
