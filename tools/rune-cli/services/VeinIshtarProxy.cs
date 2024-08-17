namespace vein.cmd;

using System.Diagnostics;

public class VeinIshtarProxy(FileInfo compilerPath, IEnumerable<string> args, DirectoryInfo baseFolder) : IDisposable
{
    private readonly Process _process = new()
    {
        StartInfo = new()
        {
            FileName = compilerPath.FullName,
            Arguments = string.Join(" ", args),
            RedirectStandardOutput = true,
            RedirectStandardError = false,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = baseFolder.FullName
        }
    };

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

        await Task.WhenAll(outputTask);

        await _process.WaitForExitAsync();

        return _process.ExitCode;   
    }

    public void Dispose() => _process.Dispose();
}
