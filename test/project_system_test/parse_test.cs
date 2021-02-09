namespace project_system_test
{
    using NuGet.Versioning;
    using Sprache;
    using wave.project;
    using Xunit;

    public class parse_test
    {
        [Fact]
        public void ParseVersion()
        {
            var result = PackageReference.Parser.Parse("FooBar,1.1.1-preview.2");
            Assert.Equal("FooBar", result.Name);
            Assert.Equal(1, result.Version.Major);
            Assert.Equal(1, result.Version.Minor);
            Assert.Equal(1, result.Version.Patch);
            Assert.Equal("preview.2", result.Version.Release);
        }
    }
}