namespace wave.runtime.emit
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics;

    [DebuggerDisplay("{ToString()}")]
    public class Unicast<TOutF, TIn> where TOutF : struct where TIn : struct
    {
        /// <summary>
        /// cast <typeparamref name="TOutF"/> to <typeparamref name="TOutF"/>
        /// </summary>
        public static TOutF operator &(Unicast<TOutF, TIn> _, TIn q) => q.To<TOutF>();

        /// <summary>
        /// cast <see cref="int"/> to <typeparamref name="TOutF"/>
        /// </summary>
        public static TOutF operator &(Unicast<TOutF, TIn> _, int q) => q.To<TOutF>();

        /// <summary>
        /// cast <typeparamref name="TOutF"/> to <typeparamref name="TIn"/>
        /// </summary>
        public static TIn operator |(Unicast<TOutF, TIn> _, TOutF q) => q.To<TIn>();

        public static implicit operator Func<TIn, TOutF>(Unicast<TOutF, TIn> u) => q => u & q;

        public static implicit operator Func<TOutF, TIn>(Unicast<TOutF, TIn> u) => q => u | q;

        public static implicit operator Func<TIn, int, TOutF>(Unicast<TOutF, TIn> u) => (q, i) => u & q;

        public static implicit operator Func<TOutF, int, TIn>(Unicast<TOutF, TIn> u) => (q, i) => u | q;

        public Func<TOutF, TIn> auto => this;

        //

        public override string ToString() => $"static_cast<{typeof(TIn).Name}, {typeof(TOutF).Name}>";
    }

    [DebuggerDisplay("{ToString()}")]
    public class Bitcast<TOut, TIn> where TOut : struct where TIn : struct
    {
        public static TOut operator &(Bitcast<TOut, TIn> _, TIn q)
        {
            // TODO FLOAT SIZE 4 BYTE
            if (typeof(TOut) == typeof(long) && typeof(TIn) == typeof(float))
                return (TOut)(object)(long)BitConverter.ToInt32(BitConverter.GetBytes((float)(object)q), 0);
            if (typeof(TOut) == typeof(ulong) && typeof(TIn) == typeof(float))
                return (TOut)(object)(ulong)BitConverter.ToInt32(BitConverter.GetBytes((float)(object)q), 0);
            if (typeof(TOut) == typeof(float) && typeof(TIn) == typeof(long))
                return (TOut)(object)BitConverter.ToSingle(BitConverter.GetBytes((long)(object)q), 0);
            if (typeof(TOut) == typeof(float) && typeof(TIn) == typeof(ulong))
                return (TOut)(object)BitConverter.ToSingle(BitConverter.GetBytes((long)(ulong)(object)q), 0);
            throw new InvalidCastException();
        }

        public override string ToString() => $"bit_cast<{typeof(TIn).Name}, {typeof(TOut).Name}>";
    }

    public static class ObjectCaster
    {
        public static T To<T>(this object @this)
        {
            if (@this == null)
                return default;
            var type = typeof(T);
            if (@this.GetType() == type)
                return (T)@this;
            var converter1 = TypeDescriptor.GetConverter(@this);
            if (converter1.CanConvertTo(type))
                return (T)converter1.ConvertTo(@this, type);
            var converter2 = TypeDescriptor.GetConverter(type);
            if (converter2.CanConvertFrom(@this.GetType()))
                return (T)converter2.ConvertFrom(@this);
            if (@this == DBNull.Value)
                return default;
            return (T)@this;
        }
    }
}