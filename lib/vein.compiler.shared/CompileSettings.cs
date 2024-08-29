namespace vein.compiler.shared;

using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Spectre.Console.Cli;

[ExcludeFromCodeCoverage]
public class CompileSettings : CommandSettings, IProjectSettingProvider
{
    [Description("Path to vproj file")]
    [CommandArgument(0, "[PROJECT]")]
    public string Project { get; set; }

    [Description("Display exported types table")]
    [CommandOption("--print-result-types")]
    public bool PrintResultType { get; set; }

    [Description("Disable optimization")]
    [CommandOption("--deopt")]
    public bool DisableOptimization { get; set; }

    [Description("Compile into single file")]
    [CommandOption("--single-file|-s")]
    public bool HasSingleFile { get; set; }

    [Description("Wait to attach debbugger (ONLY DEBUG COMPILER)")]
    [CommandOption("--sys-debugger")]
    public bool IsNeedDebuggerAttach { get; set; }
    [Description("Enable stacktrace printing when error.")]
    [CommandOption("--sys-stack-trace")]
    public bool DisplayStacktraceGenerator { get; set; }

    [Description("Generate shard package.")]
    [CommandOption("--gen-shard")]
    public bool GeneratePackageOutput { get; set; }

    [Description("Ignore cache.")]
    [CommandOption("--ignore-cache")]
    public bool IgnoreCache { get; set; }

    [Description("Override generated version")]
    [CommandOption("--override-version", IsHidden = true)]
    public string OverrideVersion { get; set; }

    [Description("Override generated version")]
    [CommandOption("--display-diagnostic-trace", IsHidden = false)]
    public bool DisplayDiagnosticTrace { get; set; }
}
