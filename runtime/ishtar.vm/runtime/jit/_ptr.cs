namespace ishtar.jit;

using System.Runtime.InteropServices;

[DebuggerDisplay("{_value}")]
public unsafe struct _ptr
{
    internal static _ptr INVALID = new _ptr();


    private byte* _value;
    internal int dataSize { get; private set; }
    internal PTR_FLAG flags { get; private set; }



    internal unsafe _ptr(void* ptr, int dataSize = 0, PTR_FLAG flags = PTR_FLAG.NONE)
        : this()
    {
        this.dataSize = dataSize;
        this._value = (byte*)ptr;
        this.flags = flags;
    }
    internal unsafe _ptr(byte* ptr, int dataSize = 0, PTR_FLAG flags = PTR_FLAG.NONE)
        : this()
    {
        this.dataSize = dataSize;
        this._value = ptr;
        this.flags = flags;
    }

    internal unsafe _ptr(IntPtr ptr, int dataSize = 0, PTR_FLAG flags = PTR_FLAG.NONE)
        : this()
    {
        this.dataSize = dataSize;
        this._value = (byte*)ptr;
        this.flags = flags;
    }

    public static _ptr operator +(_ptr p1, int offset)
        => new() { _value = p1._value + offset, dataSize = offset == 0 ? p1.dataSize : 0 };
    public static _ptr operator +(_ptr p1, _ptr p2)
        => new() { _value = p1._value + (uint)p2._value, dataSize = 0 };


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static long __max(long val1, long val2)
        => (val1 >= val2) ? val1 : val2;


    public static _ptr operator -(_ptr p1, _ptr p2)
        => new () { _value = (byte*)__max(0, p1._value - p2._value), dataSize = 0 };

    public static _ptr operator -(_ptr p1, int offset)
        => new () { _value = p1._value - offset, dataSize = offset == 0 ? p1.dataSize : 0 };

    public static bool operator >(_ptr p1, _ptr p2)
        => p1._value > p2._value;

	public static bool operator <(_ptr p1, _ptr p2)
        => !(p1 > p2);

    public static bool operator <=(_ptr p1, _ptr p2)
        => (p1 < p2) || (p1 == p2);

    public static bool operator >=(_ptr p1, _ptr p2)
        => (p1 > p2) || (p1 == p2);

    public static bool operator ==(_ptr p1, _ptr p2)
        => p1._value == p2._value && p1.dataSize == p2.dataSize;

	public static bool operator !=(_ptr p1, _ptr p2)
        => !(p1 == p2);

    public static implicit operator IntPtr(_ptr p)
		=> (IntPtr)p._value;

	public static explicit operator long(_ptr p)
	    => (long)(ulong)p._value;

	public static implicit operator _ptr(IntPtr p)
        => new (p);


    public static unsafe implicit operator _ptr(void* p)
        => new (p);

    internal bool Equals(_ptr other)
        => new IntPtr(_value).Equals(new IntPtr(other._value));

	public override bool Equals(object obj)
	{
		if (ReferenceEquals(null, obj)) return false;
		return obj is _ptr ptr && Equals(ptr);
	}

	public override int GetHashCode()
		=> new IntPtr(_value).GetHashCode();

    internal void SetUI8(byte value, int index = 0)
        => *(_value + index) = value;
	internal void SetI8(sbyte value, int index = 0)
		=> *((sbyte*)_value + index) = value;

	internal void SetI16(short value, int index = 0)
		=> *((short*)_value + index) = value;

	internal void SetUI16(ushort value, int index = 0)
		=> *((ushort*)_value + index) = value;

	internal void SetI32(int value, int index = 0)
		=> *((int*)_value + index) = value;
    internal void SetUI32(uint value, int index = 0)
		=> *((uint*)_value + index) = value;

	internal void SetI64(long value, int index = 0)
		=> *((long*)_value + index) = value;

	internal void SetUI64(ulong value, int index = 0)
		=> *((ulong*)_value + index) = value;

	internal void SetF32(float value, int index = 0)
		=> *((float*)_value + index) = value;

	internal void SetF64(double value, int index = 0)
		=> *((double*)_value + index) = value;

	internal byte GetUI8(int index = 0)
		=> *(_value + index);
	internal sbyte GetI8(int index = 0)
		=> *((sbyte*)_value + index);

	internal short GetI16(int index = 0)
		=> *((short*)_value + index);

	internal ushort GetUI16(int index = 0)
		=> *((ushort*)_value + index);

	internal int GetI32(int index = 0)
		=> *((int*)_value + index);

	internal uint GetUI32(int index = 0)
		=> *((uint*)_value + index);

	public override string ToString()
		=> $"0x{(long)_value:X}".Trim('{', '}');


    internal T ToCallable<T>(Type delegateType)
    {
        var fn = Marshal.GetDelegateForFunctionPointer(this, delegateType);
        //var fd = DelegateCreator.CreateCompatibleDelegate<T>(fn, fn.Method);
        return default;
    }
}
