namespace wave.emit.opcodes
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.InteropServices;

    public unsafe class LDC_I_X<TIntValue> : Fragment, IArgs where TIntValue : unmanaged, IFormattable, IConvertible, IComparable
    {
        private readonly TIntValue _value;

        public LDC_I_X(TIntValue value) : base(OpCodeValues.ldc_iX) 
            => _value = value;

        protected override string ToTemplateString() 
            => $":ldc.i {_value:X8}";

        public byte[] Get()
        {
            var len = (byte)sizeof(TIntValue);
            var list = new List<byte>
            {
                len
            };
            list.AddRange(getBytes(_value));
            return list.ToArray();
        }

        private static IEnumerable<byte> getBytes<T>(T str) where T : unmanaged
        {
            var size = sizeof(T);
            var arr = new byte[size];
            var ptr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(str, ptr, true);
            Marshal.Copy(ptr, arr, 0, size);
            Marshal.FreeHGlobal(ptr);
            return arr;
        }
    }
}