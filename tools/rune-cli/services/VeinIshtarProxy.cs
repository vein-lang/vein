namespace vein.cmd;

public class VeinIshtarProxy(FileInfo compilerPath, IEnumerable<string> args, DirectoryInfo baseFolder, Dictionary<string, string> env) : IDisposable
{
    private readonly Process _process = new()
    {
        StartInfo = CreateProcess(compilerPath, args, baseFolder, env)
    };


    private static ProcessStartInfo CreateProcess(FileInfo compilerPath, IEnumerable<string> args, DirectoryInfo baseFolder, Dictionary<string, string> env)
    {
        var p = new ProcessStartInfo
        {
            FileName = compilerPath.FullName,
            Arguments = string.Join(" ", args),
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = baseFolder.FullName
        };
        foreach (var (k, v) in env)
            p.EnvironmentVariables.Add(k, v);
        return p;
    }

    public async ValueTask<int> ExecuteAsync()
    {
        var outputTask = Task.Run(async () =>
        {
            _process.Start();
            using var reader = _process.StandardOutput;
            char[] buffer = new char[1024];
            int charsRead;
            while ((charsRead = await reader.ReadAsync(buffer, 0, buffer.Length)) > 0)
                await Console.Out.WriteAsync(new string(buffer, 0, charsRead));
        });

        var errorTask = Task.Run(async () =>
        {
            _process.Start();
            using var reader = _process.StandardError;
            char[] buffer = new char[1024];
            int charsRead;
            while ((charsRead = await reader.ReadAsync(buffer, 0, buffer.Length)) > 0)
                await Console.Error.WriteAsync(new string(buffer, 0, charsRead));
        });

        await Task.WhenAll(outputTask, errorTask);

        await _process.WaitForExitAsync();

        return _process.ExitCode;   
    }

    public void Dispose() => _process.Dispose();
}
