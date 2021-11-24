namespace ishtar.jit;

using System.Runtime.InteropServices;
using linux;

public class _constants
{
    public static readonly bool X64 = IntPtr.Size > 4;
    public const int INVALID_ID = -1;

    public static long CoreCount
    {
        get
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                return syscall.sysconf(syscall.SysConfKind._SC_NPROCESSORS_ONLN);
            return 1; // TODO
        }
    }
}
