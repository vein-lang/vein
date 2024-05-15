namespace ishtar.vm;

public readonly unsafe struct IshtarVersion(uint major, uint minor, uint patch = 0, uint build = 0)
{
    public uint Major { get; } = major;
    public uint Minor { get; } = minor;
    public uint Patch { get; } = patch;
    public uint Build { get; } = build;

    public bool Equals(IshtarVersion* ver) => ver->Major == Major && ver->Minor == Minor && ver->Patch == Patch && ver->Build == Build;
    public bool Equals(IshtarVersion ver) => ver.Major == Major && ver.Minor == Minor && ver.Patch == Patch && ver.Build == Build;

    public static IshtarVersion Parse(string str)
    {
        var mv = Version.Parse(str);
        return new IshtarVersion((uint)mv.Major, (uint)mv.Minor, (uint)mv.Revision, (uint)mv.Build);
    }
}
