namespace vein.syntax
{
    using Sprache;
    using System.Collections.Generic;
    using System.Linq;

    public static class Combinators
    {
        public static Parser<IEnumerable<T>> ChainForward<T, Z>(this Parser<T> elm, Parser<Z> dlm)
            where T : IPositionAware<T> =>
            elm
            .Positioned()
            .Once()
            .Then(x => dlm
                .Token()
                .Then(_ => elm.Token()
                    .Positioned())
                .Many()
                .Select(x.Concat));
    }
}
