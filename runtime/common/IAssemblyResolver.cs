namespace vein;

using System;
using System.Collections.Generic;
using runtime;

public interface IAssemblyResolver
{
    VeinModule ResolveDep(string name, Version version, IReadOnlyList<VeinModule> deps);
}
