[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("wc_test")]
namespace wave.extensions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public static class EnumExtension
    {
        public static IEnumerable<TEnum> EnumerateFlags<TEnum>(this TEnum flags) where TEnum : struct
        {
            if (!typeof(TEnum).IsEnum)
                throw new ArgumentException();

            var buff = (Enum)(ValueType)flags;

            return Enum.GetValues(typeof(TEnum)).Cast<Enum>().Where(x => buff.HasFlag(x)).Cast<TEnum>();
        }
    }
    
    public static class EnumerableExtension
    {
        public static string Join<T>(this IEnumerable<T> t, char joint) => string.Join(joint, t);
        public static string Join<T>(this IEnumerable<T> t, string joint) => string.Join(joint, t);
    }
}