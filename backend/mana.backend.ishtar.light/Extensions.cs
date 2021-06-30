namespace ishtar
{
    using System;
    using System.Collections.Generic;
    using mana.exceptions;
    using mana.runtime;

    public static class Extensions
    {
        public static IEnumerable<RuntimeIshtarMethod> FindMethods(this IEnumerable<RuntimeIshtarInterface> @interfaces, string fullyName)
        {
            var list = new List<RuntimeIshtarMethod>();
            foreach (var @interface in @interfaces)
            {
                var result = @interface.FindMethod(fullyName, x => x.Name.Equals(fullyName));
                if (result is null)
                    continue;
                if (result is not RuntimeIshtarMethod m)
                    throw new TypeMismatchException();
                list.Add(m);
            }
            return list;
        }
    }
}
