namespace ishtar.jit.linux;

using System.Runtime.InteropServices;

public static class syscall
{
    internal const string LIBC = "libc";

    [DllImport (LIBC, SetLastError=true)]
    public static extern int wait(out int status);

    // TODO
    public static long sysconf(SysConfKind name, int defaultError = 0)
    {
        if (name == SysConfKind._SC_NPROCESSORS_ONLN)
            return 1;
        throw new Exception();
    }


    public enum SysConfKind
    {
        _SC_NPROCESSORS_ONLN
    }
}



