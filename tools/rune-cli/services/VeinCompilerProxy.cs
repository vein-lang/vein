namespace vein.cmd;

using System.Diagnostics;
using Spectre.Console;

public class VeinCompilerProxy(FileInfo compilerPath, IEnumerable<string> args) : IDisposable
{
    private readonly Process _process = new()
    {
        StartInfo = new ProcessStartInfo
        {
            FileName = compilerPath.FullName,
            Arguments = string.Join(" ", args),
            RedirectStandardOutput = true,
            RedirectStandardError = false,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = Directory.GetCurrentDirectory(),
            EnvironmentVariables =
            {
                { "FORK_CONSOLE", "true" },
                { "FORK_CONSOLE_W", (ConsoleHelper.GetSafeWidth() - 1).ToString() },
                { "FORK_CONSOLE_H", (ConsoleHelper.GetSafeHeight() - 1).ToString() }
            }
        }
    };

    public async ValueTask<int> ExecuteAsync()
    {
        if (Environment.GetEnvironmentVariable("NO_CONSOLE") is not null || Environment.GetEnvironmentVariable("NO_COLOR") is not null)
            _process.StartInfo.EnvironmentVariables.Add("NO_CONSOLE", "NO_COLOR");

        var outputTask = Task.Run(async () =>
        {
            _process.Start();
            using var reader = _process.StandardOutput;
            char[] buffer = new char[1024];
            int charsRead;
            while ((charsRead = await reader.ReadAsync(buffer, 0, buffer.Length)) > 0)
                await Console.Out.WriteAsync(new string(buffer, 0, charsRead));
        });

        await Task.WhenAll(outputTask);

        await _process.WaitForExitAsync();

        return _process.ExitCode;
    }

    public void Dispose() => _process.Dispose();
}
