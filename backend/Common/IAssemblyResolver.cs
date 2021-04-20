namespace wave
{
    using System;
    using System.Collections.Generic;
    using runtime;

    public interface IAssemblyResolver
    {
        WaveModule ResolveDep(string name, Version version, List<WaveModule> deps);
    }
}