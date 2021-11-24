namespace ishtar.jit;

using System.ComponentModel;

internal class RegisterCount
{
    public static RegisterCount SystemDefault
    {
        get
        {
            var rc = new RegisterCount
            {
                Mm = 8,
                K = 8,
                Gp = 16,
                Xyz = 16
            };
            return rc;
        }
    }

    public int Gp { get; private set; }
    public int Mm { get; private set; }
    public int K { get; private set; }
    public int Xyz { get; private set; }

    public void CopyFrom(RegisterCount m)
    {
        Gp = m.Gp;
        Mm = m.Mm;
        K = m.K;
        Xyz = m.Xyz;
    }

    public void Add(RegisterClass cls, int n = 1)
    {
        switch (cls)
        {
            case RegisterClass.Gp:
                Gp += n;
                break;
            case RegisterClass.Mm:
                Mm += n;
                break;
            case RegisterClass.K:
                K += n;
                break;
            case RegisterClass.Xyz:
                Xyz += n;
                break;
            default:
                throw new InvalidEnumArgumentException();
        }
    }

    public int Get(RegisterClass cls) => cls switch
    {
        RegisterClass.Gp => Gp,
        RegisterClass.Mm => Mm,
        RegisterClass.K => K,
        RegisterClass.Xyz => Xyz,
        _ => throw new InvalidEnumArgumentException(),
    };

    public void Set(RegisterClass cls, int v)
    {
        switch (cls)
        {
            case RegisterClass.Gp:
                Gp = v;
                break;
            case RegisterClass.Mm:
                Mm = v;
                break;
            case RegisterClass.K:
                K = v;
                break;
            case RegisterClass.Xyz:
                Xyz = v;
                break;
            default:
                throw new InvalidEnumArgumentException();
        }
    }

    public void IndexFrom(RegisterCount count)
    {
        var x = count.Gp;
        Gp = 0;
        Mm = x;
        x += count.Mm;
        var y = x + count.K;
        K = x;
        Xyz = y;
    }

    public void Reset()
    {
        Gp = 0;
        Mm = 0;
        K = 0;
        Xyz = 0;
    }

    public override string ToString()
        => string.Format("Gp={0}, Mm={1}, K={2}, Xyz={3}", Gp, Mm, K, Xyz);
}