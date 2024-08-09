namespace ishtar;

[CTypeExport("ishtar_version_t")]
public readonly unsafe struct IshtarVersion(uint major, uint minor, uint patch = 0, uint build = 0)
{
    public uint Major => major;
    public uint Minor => minor;
    public uint Patch => patch;
    public uint Build => build;

    public bool Equals(IshtarVersion* ver) => ver->Major == Major && ver->Minor == Minor && ver->Patch == Patch && ver->Build == Build;
    public bool Equals(IshtarVersion ver) => ver.Major == Major && ver.Minor == Minor && ver.Patch == Patch && ver.Build == Build;

    public static IshtarVersion Parse(string str)
    {
        var mv = Version.Parse(str);
        return new IshtarVersion((uint)mv.Major, (uint)mv.Minor, (uint)mv.Revision, (uint)mv.Build);
    }

    public override string ToString()
        => $"{major}.{minor}.{patch}.{build}";

    public override int GetHashCode() => HashCode.Combine(Major, Minor, Patch, Build);
}
