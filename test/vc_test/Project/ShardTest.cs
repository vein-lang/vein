namespace veinc_test.Project;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NuGet.Versioning;
using vein.project;
using vein.project.shards;

public class ShardTest
{
    public static readonly PackageManifest Manifest = new PackageManifest()
    {
        Authors = { "Yuuki Wesp" },
        BugUrl = new Uri("https://foobar.test"),
        Description = "FooBar",
        Name = "foo-bar",
        Version = new NuGetVersion(1, 2, 3, 4),
        License = "MIT",
        IsPreview = false
    };

    [Test]
    public Task ShardCreateTest() => _createShard();

    [Test]
    public async Task ValidateShardTest()
    {
        var info = await _createShard();

        var shard = await Shard.OpenAsync(info);

        Assert.AreEqual(Manifest.Name, shard.Name);
        Assert.AreEqual($"{Manifest.Version}", $"{shard.Version}");
        Assert.AreEqual(Manifest.Description, shard.Description);

        var files = shard.GetFiles("lib");

        Assert.AreEqual(1, files.Count());
    }


    private async Task<FileInfo> _createShard()
    {
        var text1 = new FileInfo(Path.Combine(Path.GetTempPath(), "veinc_test", Path.GetTempFileName()));
        var output_arhive = new FileInfo(Path.Combine(Path.GetTempPath(), "veinc_test", Path.GetTempFileName()));

        File.WriteAllText(text1.FullName, "foo\nbar");

        await new ShardBuilder(Manifest, Console.WriteLine)
            .Storage()
            .File(text1)
            .Folder("lib", x => x.File(text1))
            .Return()
            .Save(output_arhive);

        return output_arhive;
    }
}
