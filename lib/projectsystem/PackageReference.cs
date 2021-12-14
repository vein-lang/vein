namespace vein.project
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Newtonsoft.Json;
    using NuGet.Versioning;
    using Sprache;

    public interface IProjectRef { }

    public record ProjectReference(string path) : IProjectRef
    {
        public VeinProject ReadProject(VeinProject owner)
            => VeinProject.LoadFrom(new FileInfo(Path.Combine(owner.WorkDir.FullName, path)));
    }

    public record PackageReference(string Name, [JsonConverter(typeof(NuGetVersionConverter))] NuGetVersion Version)
        : IProjectRef, IDependency
    {

        internal static Parser<string> RawIdentifier =>
            from identifier in Parse.Identifier(Parse.Letter, Parse.LetterOrDigit
                .Or(Parse.Chars('_', '.', '-')))
            select identifier;

        internal static Parser<string> Identifier =>
            RawIdentifier.Token().Named("Identifier");

        internal static Parser<string> VersionNumber =>
            from number in Parse.Number.AtLeastOnce()
            select string.Join("", number);

        internal static Parser<(string releaseLabel, string metadata)> PreviewVersion =>
            from ch in Parse.Char('-')
            from name in Parse.Identifier(Parse.Letter, Parse.LetterOrDigit)
            from meta in (
                from dot in Parse.Char('.')
                from number in Parse.Numeric.Many().Text()
                select number).Optional()
            select (name, meta.GetOrDefault());

        internal static Parser<NuGetVersion> VersionParse =>
            from major in VersionNumber.Token()
            from c1 in Parse.Char('.')
            from minor in VersionNumber.Token()
            from c2 in Parse.Char('.').Optional()
            from build in VersionNumber.Token().Optional()
            from c3 in Parse.Char('.').Optional()
            from revision in VersionNumber.Token().Optional()
            from preview in PreviewVersion.Optional()
            select new NuGetVersion(
                P(major), P(minor),
                P(build), P(revision),
                P(preview), "");


        internal static Parser<PackageReference> Parser =>
            from id in Identifier
            from comma in Parse.Char(',')
            from vers in VersionParse
            select new PackageReference(id, vers);

        string IDependency.Name => Name;
        Version IDependency.Version => Version.Version;

        public static IProjectRef Convert(string t)
        {
            if (string.IsNullOrEmpty(t))
                throw new ArgumentNullException(nameof(t));
            if (t.EndsWith(".vproj"))
                return new ProjectReference(t);
            return Parser.Parse(t);
        }


        private static IEnumerable<string> P(IOption<(string, string)> s) =>
            new[] { s.GetOrDefault().Item1, s.GetOrDefault().Item2 }.AsEnumerable();

        private static int P(string s)
            => int.Parse(s);
        private static int P(IOption<string> s)
            => s.IsDefined ? P(s.Get()) : 0;
    }
}
