namespace vein.cmd;

public class VeinIshtarProxy(FileInfo compilerPath, IEnumerable<string> args, DirectoryInfo baseFolder, Dictionary<string, string> env, bool redirectStdout) : IDisposable
{
    private readonly Process _process = new()
    {
        StartInfo = CreateProcess(compilerPath, args, baseFolder, env, redirectStdout)
    };


    private static ProcessStartInfo CreateProcess(FileInfo compilerPath, IEnumerable<string> args, DirectoryInfo baseFolder, Dictionary<string, string> env, bool redirectStdout)
    {
        var p = new ProcessStartInfo
        {
            FileName = compilerPath.FullName,
            Arguments = string.Join(" ", args),
            RedirectStandardOutput = redirectStdout,
            RedirectStandardError = redirectStdout,
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
        var cts = new CancellationTokenSource();
        Console.CancelKeyPress += ConsoleOnCancelKeyPress;

        void ConsoleOnCancelKeyPress(object? sender, ConsoleCancelEventArgs e)
        {
            cts.Cancel(false);
            _process.Kill();
        }
        
        if (redirectStdout)
        {
            async ValueTask redirect(Func<StreamReader> getReader)
            {
                using var reader = getReader();
                char[] buffer = new char[1024];
                int charsRead;
                while ((charsRead = await reader.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    await Console.Out.WriteAsync(new string(buffer, 0, charsRead));
            }

            var outputTask = Task.Run(() => redirect(() => _process.StandardOutput), cts.Token);
            var errorTask = Task.Run(() => redirect(() =>  _process.StandardError), cts.Token);

            _process.Start();

            await Task.WhenAll(outputTask, errorTask);
        }
        else
            _process.Start();

        await _process.WaitForExitAsync(cts.Token);

        Console.CancelKeyPress -= ConsoleOnCancelKeyPress;
        
        return _process.ExitCode;   
    }

    public void Dispose() => _process.Dispose();
}
