namespace ishtar.runtime;

public readonly struct RuntimeInfo()
{
    public readonly bool isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
    public readonly bool isLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
    public readonly bool isOSX = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
    public readonly bool isFreeBSD = RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD);
    public readonly Architecture Architecture = RuntimeInformation.OSArchitecture;
}
