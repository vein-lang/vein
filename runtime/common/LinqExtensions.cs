namespace vein;

using System;
using System.Collections.Generic;

public static class LinqExtensions
{
    public static void ForEach<T>(this IEnumerable<T> enumerator, Action<T> actor)
    {
        foreach (var v in enumerator) actor(v);
    }

    public static IEnumerable<T> Pipe<T>(this IEnumerable<T> enumerator, Action<T> actor)
    {
        foreach (var v in enumerator)
        {
            actor(v);
            yield return v;
        }
    }
}
