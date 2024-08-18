namespace vein;

using System.Net.Http.Headers;
using Flurl.Http;
using Flurl;

public static class DownloadExtensions
{
    /// <summary>
    /// Asynchronously downloads a file at the specified URL with progress reporting.
    /// </summary>
    /// <param name="request">The Flurl request.</param>
    /// <param name="localFolderPath">Path of local folder where file is to be downloaded.</param>
    /// <param name="localFileName">Name of local file. If not specified, the source filename (from Content-Disposition header, or last segment of the URL) is used.</param>
    /// <param name="bufferSize">Buffer size in bytes. Default is 4096.</param>
    /// <param name="completionOption">The HttpCompletionOption used in the request. Optional.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <param name="progress">Progress reporting delegate.</param>
    /// <returns>A Task whose result is the local path of the downloaded file.</returns>
    public static async Task<string> DownloadFileAsync(
        this IFlurlRequest request,
        string localFolderPath,
        string localFileName = null,
        int bufferSize = 4096,
        HttpCompletionOption completionOption = HttpCompletionOption.ResponseHeadersRead,
        CancellationToken cancellationToken = default,
        IProgress<(int percentComplete, int speed)> progress = null)
    {
        using IFlurlResponse resp = await request.SendAsync(HttpMethod.Get, completionOption: completionOption, cancellationToken: cancellationToken).ConfigureAwait(false);
        if (localFileName == null)
            localFileName = GetFileNameFromHeaders(resp.ResponseMessage) ?? GetFileNameFromPath(request);

        await using Stream httpStream = await resp.GetStreamAsync().ConfigureAwait(false);
        var filePath = FileUtil.CombinePath(localFolderPath, localFileName);
        var fileLength = resp.ResponseMessage.Content.Headers.ContentLength ?? -1L;

        await using Stream fileStream = await FileUtil.OpenWriteAsync(localFolderPath, localFileName, bufferSize).ConfigureAwait(false);
        await CopyToWithProgressAsync(httpStream, fileStream, bufferSize, fileLength, cancellationToken, progress).ConfigureAwait(false);

        return filePath;
    }

    private static async Task CopyToWithProgressAsync(Stream source, Stream destination, int bufferSize, long fileLength, CancellationToken cancellationToken, IProgress<(int percentComplete, int speed)> progress)
    {
        var buffer = new byte[bufferSize];
        long totalBytesRead = 0;
        long lastReportedBytes = 0;
        DateTime lastReportedTime = DateTime.UtcNow;

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        int bytesRead;
        while ((bytesRead = await source.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false)) > 0)
        {
            await destination.WriteAsync(buffer, 0, bytesRead, cancellationToken).ConfigureAwait(false);
            totalBytesRead += bytesRead;

            // Calculate elapsed time
            var elapsed = stopwatch.Elapsed;
            stopwatch.Restart();

            // Report progress
            if (fileLength > 0)
            {
                int percentComplete = (int)((totalBytesRead * 100) / fileLength);
                int speed = (int)((totalBytesRead - lastReportedBytes) / elapsed.TotalSeconds);
                progress?.Report((percentComplete, speed));
                lastReportedBytes = totalBytesRead;
            }
            else
                progress?.Report((0, 0)); // Report 0% and 0 speed if fileLength is unknown

            lastReportedTime = DateTime.UtcNow;
        }

        // Final progress report if fileLength was unknown
        if (fileLength <= 0)
        {
            int percentComplete = totalBytesRead > 0 ? 100 : 0;
            progress?.Report((percentComplete, 0));
        }
    }

    private static string GetFileNameFromHeaders(HttpResponseMessage resp)
    {
        ContentDispositionHeaderValue contentDisposition = resp.Content?.Headers.ContentDisposition;
        if (contentDisposition == null)
            return null;
        string fileName = contentDisposition.FileNameStar ?? contentDisposition.FileName;
        return fileName != null ? fileName.Trim('"') : null;
    }

    private static string GetFileNameFromPath(IFlurlRequest req) => Url.Decode(req.Url.Path.Split('/').Last(), false);
}

internal static class FileUtil
{
    internal static string GetFileName(string path) => Path.GetFileName(path);

    internal static string CombinePath(params string[] paths) => Path.Combine(paths);

    internal static Task<Stream> OpenReadAsync(string path, int bufferSize)
    {
        return Task.FromResult<Stream>((Stream)new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize, true));
    }

    internal static Task<Stream> OpenWriteAsync(string folderPath, string fileName, int bufferSize)
    {
        Directory.CreateDirectory(folderPath);
        return Task.FromResult<Stream>((Stream)new FileStream(Path.Combine(folderPath, fileName), FileMode.Create, FileAccess.Write, FileShare.None, bufferSize, true));
    }

    /// <summary>Replaces invalid path characters with underscores.</summary>
    internal static string MakeValidName(string s)
    {
        return string.Join("_", s.Split(Path.GetInvalidFileNameChars()));
    }
}
