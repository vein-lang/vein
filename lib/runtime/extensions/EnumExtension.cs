[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("wc_test")]
namespace wave.extensions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using runtime.emit;

    public static class EnumExtension
    {
        public static IEnumerable<TEnum> EnumerateFlags<TEnum>(this TEnum flags) where TEnum : struct
        {
            if (!typeof(TEnum).IsEnum)
                throw new ArgumentException();

            var buff = (Enum)(ValueType)flags;

            return Enum.GetValues(typeof(TEnum)).Cast<Enum>().Where(x => buff.HasFlag(x)).Cast<TEnum>();
        }

        public static bool InRange(this Range range, int value) 
            => value >= range.Start.Value && value <= range.End.Value;
        public static bool InRange(this OpCode opcode, OpCodeValue start, OpCodeValue end)
            => ((ushort) start..(ushort) end).InRange(opcode.Value);
    }
    
    public static class EnumerableExtension
    {
        public static string Join<T>(this IEnumerable<T> t, char joint) => string.Join(joint, t);
        public static string Join<T>(this IEnumerable<T> t, string joint) => string.Join(joint, t);
    }
}