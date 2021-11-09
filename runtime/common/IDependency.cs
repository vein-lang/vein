namespace vein;

using System;

public interface IDependency
{
    public string Name { get; }
    public Version Version { get; }
}
