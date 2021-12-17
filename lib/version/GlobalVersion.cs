namespace vein;


/// <summary>
/// So, i hate fucking GitVersion with his constant problems
/// </summary>
public static class GlobalVersion
{
    public static string BranchName => GitVersionInformation.BranchName;
    public static string AssemblySemFileVer => GitVersionInformation.AssemblySemFileVer;
    public static string AssemblySemVer => GitVersionInformation.AssemblySemVer;
    public static string CommitDate => GitVersionInformation.CommitDate;
    public static string BuildMetaData => GitVersionInformation.BuildMetaData;
    public static string FullSemVer => GitVersionInformation.FullSemVer;
    public static string InformationalVersion => GitVersionInformation.InformationalVersion;
    public static string Major => GitVersionInformation.Major;
    public static string Minor => GitVersionInformation.Minor;
    public static string Patch => GitVersionInformation.Patch;
    public static string Sha => GitVersionInformation.Sha;
    public static string ShortSha => GitVersionInformation.ShortSha;
    public static string PreReleaseTag => GitVersionInformation.PreReleaseTag;
}
