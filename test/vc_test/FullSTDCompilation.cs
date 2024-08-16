namespace veinc_test.STD;

using System.Globalization;
using LibGit2Sharp;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using vein.cmd;
using vein.compilation;
using vein.compiler.shared;
using vein.json;
using vein.project;

[TestFixture(Ignore = "")]
public class FullSTDCompilation
{
    public const string StdUrl = "https://github.com/vein-lang/std.git";

    public DirectoryInfo cache_folder { get; set; }
    public FileInfo project_file => cache_folder.SubDirectory("src").File("std.vproj");
    [OneTimeSetUp]
    public void Setup()
    {
        cache_folder = new DirectoryInfo(Path.Combine(Path.GetTempPath(), "veinc_test_cache", Guid.NewGuid().ToString().Trim('-')));
        cache_folder.Create();
        JsonConvert.DefaultSettings = () => new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            Formatting = Formatting.Indented,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            Culture = CultureInfo.InvariantCulture,
            Converters = new List<JsonConverter>()
            {
                new FileInfoSerializer(),
                new StringEnumConverter()
            }
        };
        Environment.SetEnvironmentVariable("NO_COLOR", "1", EnvironmentVariableTarget.Process);
    }

    [OneTimeTearDown]
    public void Clean() => Assert.IsTrue(cache_folder.Exists);

    [Test, Order(1)]
    public void Clone()
    {
        Assert.IsTrue(cache_folder.Exists);
        Console.WriteLine(Repository.Clone(StdUrl, cache_folder.FullName));
    }

    [Test, Order(2)]
    public void Compile()
    {
        AppFlags.Set(ApplicationFlag.use_experimental_options, true);
        AppFlags.Set(ApplicationFlag.exp_simplify_optimize, true);
        AppFlags.Set(ApplicationFlag.use_predef_array_type_initer, true);
        var project = VeinProject.LoadFrom(project_file);
        var settings = new CompileSettings();
        var targets = CompilationTask.RunAsync(project.WorkDir, settings).Result;
        Assert.IsNotEmpty(targets);
        Assert.IsTrue(targets.Count == 1);

        var target = targets.Single();

        Assert.IsTrue(target.Status == CompilationStatus.Success);
    }
}
