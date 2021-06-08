namespace mana.syntax
{
    using Sprache;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.InteropServices;

    public static class Combinators
    {
        public static Parser<IEnumerable<T>> ChainForward<T, Z>(this Parser<T> elm, Parser<Z> dlm) where T : IPositionAware<T> =>
            elm.Positioned().Once().Then(x => dlm.Token().Then(_ => elm.Token().Positioned()).Many().Select(z => x.Concat(z)));
    }
}
