namespace mana.reflection
{
    using System.Collections.Generic;

    public interface IAspectable
    {
        List<Aspect> Aspects { get; }
    }
}
