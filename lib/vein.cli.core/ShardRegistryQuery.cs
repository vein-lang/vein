namespace vein;

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Flurl.Http;
using Newtonsoft.Json;
using project;
using project.shards;

public class ShardRegistryQuery
{
    private readonly Uri _endpoint;
    private IFlurlClient _client;
    private IShardStorage _storage;

    public ShardRegistryQuery(Uri endpoint)
    {
        var settings = new JsonSerializerSettings
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        };
        var serializer = new Flurl.Http.Newtonsoft.NewtonsoftJsonSerializer(settings);

        _endpoint = endpoint;
        _client = new FlurlClient($"{endpoint}").WithSettings(x => x.JsonSerializer = serializer);
        
    }

    public ShardRegistryQuery WithStorage(IShardStorage storage)
    {
        _storage = storage;
        return this;
    }

    public ShardRegistryQuery WithApiKey(string apiKey)
    {
        _client = _client.WithHeader("X-VEIN-API-KEY", apiKey);
        return this;
    }


    public FileInfo? GetLocalShardFile(RegistryPackage manifest)
    {
        if (!_storage.IsAvailable(manifest))
            return null;
        var path = _storage.EnsureSpace(manifest);
        var package = path.File(_storage.TemplateName(manifest));

        return package;
    }

    public async ValueTask<RegistryPackage?> DownloadShardAsync(RegistryPackage manifest,
        CancellationToken token = default, IProgress<(int total, int speed)>? progress = null)
    {
        if (_storage.IsAvailable(manifest))
        {
            progress?.Report((100, 0));
            return manifest;
        }
        var path = _storage.EnsureSpace(manifest);
        var name = manifest.Name;
        var version = manifest.Version.ToNormalizedString();

        var result = await _client
            .Request($"@/packages/{name}/{version}")
            .AllowHttpStatus("404")
            .GetAsync(cancellationToken: token);

        if (result is { StatusCode: 404 })
        {
            progress?.Report((100, 0));
            return null;
        }

        var package = path.File(_storage.TemplateName(manifest));
        var fileLengthTxt = result.Headers.FirstOrDefault(x => x.Name.Equals("Content-Length")).Value ?? "-1";
        var fileLength = long.Parse(fileLengthTxt);

        var lastReportedBytes = 0L;
        await using (var file = package.OpenWrite())
        await using (var remote = await result.GetStreamAsync())
        {
            var buffer = new byte[512];
            long totalBytesRead = 0L;
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            int bytesRead;
            while ((bytesRead = await remote.ReadAsync(buffer, 0, buffer.Length, token)) > 0)
            {
                await Task.Delay(10, token);
                await file.WriteAsync(buffer, 0, bytesRead, token);
                totalBytesRead += bytesRead;
                var elapsed = stopwatch.Elapsed;
                stopwatch.Restart();
                if (fileLength > 0)
                {
                    int percentComplete = (int)((totalBytesRead * 100) / fileLength);
                    int speed = (int)((totalBytesRead - lastReportedBytes) / elapsed.TotalSeconds);
                    progress?.Report((percentComplete, speed));
                    lastReportedBytes = totalBytesRead;
                }
                stopwatch.Restart();
            }

            if (fileLength <= 0)
            {
                int percentComplete = totalBytesRead > 0 ? 100 : 0;
                progress?.Report((percentComplete, 0));
            }
        }

        var shard = await Shard.OpenAsync(package, token);

        shard.ExtractTo(path);

        return manifest;
    }
    public async ValueTask<RegistryPackage?> DownloadShardAsync(string name, string? version,
        CancellationToken token = default, IProgress<(int total, int speed)>? progress = null)
    {
        var manifest = await FindByName(name, version, token: token);

        if (manifest is null)
            return null;

        return await DownloadShardAsync(manifest, token, progress);
    }
    public async ValueTask<RegistryPackage?> FindByName(string name, string? version, bool includeUnlisted = false,
        CancellationToken token = default)
    {
        version ??= "latest";
        var result = await _client
            .Request($"@/package/{name}/{version}")
            .SetQueryParam(nameof(includeUnlisted), includeUnlisted)
            .AllowHttpStatus("404")
            .GetAsync(cancellationToken: token);

        if (result is { StatusCode: 404 })
            return null;

        return await result.GetJsonAsync<RegistryPackage>();
    }

    public async ValueTask<(RegistryResponse response, int status)> PublishPackage(FileInfo info)
    {
        if (!info.Extension.Equals(".shard"))
            throw new ShardPackageCorruptedException($"File is not shard package.");
        var shard = await Shard.OpenAsync(info);
        var pkg = _storage.TemplateName(shard.Name, shard.Version);

        var response = await _client.Request("@/publish")
            .AllowHttpStatus("400,409,201,403,500")
            .WithTimeout(60)
            .PostMultipartAsync(x => x.AddFile(pkg, info.FullName, "binary/octet-stream", fileName: pkg));

        return (await response.GetJsonAsync<RegistryResponse>(), response.StatusCode);
    }

    public async ValueTask<List<RegistryPackage>> SearchAsync(string q,
        int skip = 0,
        int take = 20,
        bool prerelease = false,
        CancellationToken cancellationToken = default) =>
        await _client.Request("@/search/index")
            .SetQueryParam(nameof(q), q)
            .SetQueryParam(nameof(skip), skip)
            .SetQueryParam(nameof(take), take)
            .SetQueryParam(nameof(prerelease), prerelease)
            .GetJsonAsync<List<RegistryPackage>>(cancellationToken: cancellationToken);
}


public class RegistryResponse
{
    public string message { get; set; }
    public string traceId { get; set; }
}
