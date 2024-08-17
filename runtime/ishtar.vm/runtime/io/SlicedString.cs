namespace ishtar.runtime.io;

[Flags]
public enum SlicedStringFlags
{
    NONE = 0,
    CHILD = 1 << 1,
    IMMORTAL = 1 << 2,
    TEMP = 1 << 3,
}

public readonly unsafe struct SlicedString(char* ptr, uint size)
{
    public SlicedString(SlicedString other, uint from, uint to) : this(other.Ptr + from, to - from)
        => Flags = SlicedStringFlags.CHILD | other.Flags;


    public readonly char* Ptr = ptr;
    public readonly uint Size = size;
    public readonly SlicedStringFlags Flags = SlicedStringFlags.NONE;

    public override string ToString()
        => new(ptr, 0, (int)size);

    public bool IsNull() => ptr == null;
}
public static unsafe class SlicedStringEx
{
    public static SlicedString Slice(this SlicedString slice, uint from, uint to)
    {
        if (from >= to || to > slice.Size)
            throw new ArgumentOutOfRangeException();
        return new SlicedString(slice, from, to);
    }

    public static bool SlicedStringEquals(this SlicedString a, SlicedString b)
    {
        if (a.Size != b.Size) return false;
        for (uint i = 0; i < a.Size; i++)
        {
            if (a.Ptr[i] != b.Ptr[i]) return false;
        }
        return true;
    }
    public static bool SlicedStringEquals(this SlicedString a, string target)
    {
        fixed (char* r = target)
        {
            var b = new SlicedString(r, (uint)target.Length);

            if (a.Size != b.Size) return false;
            for (uint i = 0; i < a.Size; i++)
            {
                if (a.Ptr[i] != b.Ptr[i]) return false;
            }
            return true;
        }
    }
}
