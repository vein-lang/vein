namespace vein.extensions
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

            return Enum.GetValues(typeof(TEnum)).Cast<TEnum>();
        }

        public static IEnumerable<int> GetEnumerable(this Range i) =>
            Enumerable.Range(i.Start.Value, i.End.Value);
        public static IEnumerator<int> GetEnumerator(this Range i) =>
            Enumerable.Range(i.Start.Value, i.End.Value).GetEnumerator();
        public static bool InRange(this Range range, int value)
            => value >= range.Start.Value && value <= range.End.Value;
    }
}
