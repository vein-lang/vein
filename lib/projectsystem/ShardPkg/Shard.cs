namespace vein.project.shards;

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;
using MoreLinq;
using Newtonsoft.Json;
using NuGet.Versioning;
using PgpCore;

public class Shard : IDisposable, IAsyncDisposable
{
    private PackageArchive _archive;
    private PackageManifest _manifest;
    private PackageMetadata _metadata;

    private bool isSigned;


    public NuGetVersion Version => _manifest.Version;
    public string Name => _manifest.Name;
    public string Description => _manifest.Description;
    public List<PackageReference> Dependencies => _manifest.Dependencies;


    public bool IsSigned => isSigned;

    public long Size => _archive.Size;


    /// <exception cref="ShardPackageCorruptedException"></exception>
    public async Task<PackageManifest> GetManifestAsync(CancellationToken token = default)
    {
        if (_manifest is not null)
            return _manifest;
        return _manifest = await GetJsonFile<PackageManifest>("manifest.json", token);
    }

    /// <exception cref="ShardPackageCorruptedException"></exception>
    public async Task<PackageMetadata> GetMetadataAsync(CancellationToken token = default)
    {
        if (_metadata is not null)
            return _metadata;

        return _metadata = await GetJsonFile<PackageMetadata>("metadata.json", token);
    }

    /// <exception cref="ShardPackageCorruptedException"></exception>
    public async Task<Stream> GetCertificate(CancellationToken token = default)
        => await GetStream("sign.cert", token);

    /// <exception cref="ShardPackageCorruptedException"></exception>
    public async Task<Stream> GetReadmeAsync(CancellationToken token = default)
        => await GetStream("readme.md", token);

    /// <exception cref="ShardPackageCorruptedException"></exception>
    public async Task<Stream> GetIconAsync(CancellationToken token = default)
        => await GetStream("icon.png", token);

    public Task<byte[]> GetContent(FileInfo info)
    {
        try
        {
            return GetBytes(info.FullName);
        }
        catch (Exception e)
        {
            throw new ShardPackageCorruptedException(e);
        }
    }

    public IEnumerable<string> GetFiles(string folder)
        => _archive.GetFiles(folder);

    /// <exception cref="ShardPackageCorruptedException"></exception>
    public static async Task<Shard> OpenAsync(Stream stream, bool leaveStreamOpen = false, CancellationToken token = default)
    {
        try
        {
            var shard = new Shard();

            shard._archive = new PackageArchive(stream);

            await shard.GetManifestAsync(token);
            //await shard.GetMetadataAsync(token);

            return shard;
        }
        catch (Exception e)
        {
            throw new ShardPackageCorruptedException(e);
        }
    }

    /// <exception cref="ShardPackageCorruptedException"></exception>
    /// <exception cref="FileNotFoundException"></exception>
    public static async Task<Shard> OpenAsync(FileInfo info, CancellationToken token = default)
    {
        if (!info.Exists)
            throw new FileNotFoundException($"File '{info.FullName}' is not found.");

        try
        {
            var shard = new Shard();

            shard._archive = new PackageArchive(info);

            await shard.GetManifestAsync(token);
            //await shard.GetMetadataAsync(token);

            return shard;
        }
        catch (Exception e)
        {
            throw new ShardPackageCorruptedException(e);
        }
    }

    public void ExtractTo(DirectoryInfo dir)
        => _archive.ExtractTo(dir);


    public static Task VerifySign(Shard shard)
    {
        shard.isSigned = false;
        return Task.CompletedTask;
    }


    private Shard() { }


    private async Task<T> GetJsonFile<T>(string name, CancellationToken token = default) where T : class
    {
        try
        {
            using var stream = _archive.GetStream(_archive.GetFile(name));
            using TextReader txt = new StreamReader(stream);

            var text = await txt.ReadToEndAsync(); // why ReadToEndAsync not pass CancellationToken? :(

            return JsonConvert.DeserializeObject<T>(text);
        }
        catch (ShardPackageCorruptedException e)
        {
            throw e;
        }
        catch (Exception e)
        {
            throw new ShardPackageCorruptedException(e);
        }
    }

    private async Task<byte[]> GetBytes(string name, CancellationToken token = default)
    {
        try
        {
            using var stream = _archive.GetStream(_archive.GetFile(name));
            using var mem = new MemoryStream();
            await stream.CopyToAsync(mem, token);
            return mem.ToArray();
        }
        catch (Exception e)
        {
            throw new ShardPackageCorruptedException(e);
        }
    }

    private async Task<MemoryStream> GetStream(string name, CancellationToken token = default)
    {
        try
        {
            using var stream = _archive.GetStream(_archive.GetFile(name));
            var mem = new MemoryStream();
            await stream.CopyToAsync(mem, token);
            return mem;
        }
        catch (Exception e)
        {
            throw new ShardPackageCorruptedException(e);
        }
    }

    private async Task CollectBytesFromFileAsync(string info, MemoryStream stream, CancellationToken token = default)
    {
        try
        {
            using var _f = _archive.GetStream(info);
            await _f.CopyToAsync(stream, token);
        }
        catch (Exception e)
        {
            throw new ShardPackageCorruptedException(e);
        }
    }


    public void Dispose() => _archive?.Dispose();

    public ValueTask DisposeAsync()
    {
        _archive?.Dispose();
        return ValueTask.CompletedTask;
    }
}


public class ShardBuilder
{
    internal readonly PackageManifest _manifest;
    internal readonly Action<string> _logger;
    internal PackageCertificate _cert;
    internal EncryptionKeys _key;
    internal ShardFileStorage _store { get; }
    internal Dictionary<FileInfo, string> signatures { get; } = new();


    public ShardBuilder(PackageManifest manifest, Action<string> logger)
    {
        _manifest = manifest;
        _logger = logger ?? (s => { });
        _store = new ShardFileStorage(this);
    }

    public ShardBuilder WithCertificate(string publicKey)
    {
        if (string.IsNullOrEmpty(publicKey))
            throw new ArgumentNullException(nameof(publicKey));
        if (!publicKey.StartsWith("-----BEGIN PGP PUBLIC KEY BLOCK-----"))
            throw new ArgumentException("Armor invalid", nameof(publicKey));
        _cert = new PackageCertificate(publicKey);
        _key = new EncryptionKeys(publicKey, null, null);
        _logger($"PGP key '{_key.PublicKey.KeyId:X}' success loaded.");
        return this;
    }

    public ShardFileStorage Storage() => _store;


    public Task Save(FileInfo file)
    {
        using var archive = new ZipArchive(file.Create(), ZipArchiveMode.Create);

        Generate(_store, archive);

        using var manifest = archive.CreateEntry("manifest.json").Open();
        using var writer = new StreamWriter(manifest);
        return writer.WriteAsync(JsonConvert.SerializeObject(_manifest, Formatting.Indented));
    }

    private void Generate(IStorageEntity entity, ZipArchive archive, string additionalPath = "")
    {
        if (entity is StorageFile file)
        {
            var output = additionalPath == "" ? file.Name : $"{additionalPath}/{file.Name}";
            archive.CreateEntryFromFile(file.info.FullName, output);
        }
        if (entity is StorageFolder folder)
        {
            foreach (var e in folder.InnerEntities)
                Generate(e, archive, additionalPath == "" ? folder.Name : $"{additionalPath}/{folder.Name}");
        }
        if (entity is ShardFileStorage store)
        {
            foreach (var e in store.InnerEntities)
                Generate(e, archive);
        }
    }
}


public class ShardFileStorage : IStorageEntity
{
    internal readonly ShardBuilder _builder;
    internal List<IStorageEntity> _entities = new();
    internal ShardFileStorage(ShardBuilder builder) => _builder = builder;

    public string Name => "<root>";

    public IEnumerable<IStorageEntity> InnerEntities
        => _entities;

    public IStorageEntity File(FileInfo path)
        => this.File(path, true);

    public IStorageEntity File(FileInfo path, bool conditional)
    {
        if (conditional)
            _entities.Add(new StorageFile(this, path));
        return this;
    }

    public IStorageEntity Files(FileInfo[] paths)
    {
        paths.Pipe(x => _entities.Add(new StorageFile(this, x))).Consume();
        return this;
    }

    public IStorageEntity Folder(string folderName, Action<IStorageEntity> actor)
    {
        var entity = new StorageFolder(this, folderName);
        _entities.Add(entity);
        actor(entity);
        return this;
    }

    public ShardBuilder Return() => _builder;
}


public record struct StorageFolder(ShardFileStorage storage, string Name) : IStorageEntity
{
    internal List<IStorageEntity> _entities = new();

public IStorageEntity File(FileInfo path)
    => this.File(path, true);

public IStorageEntity File(FileInfo path, bool conditional)
{
    if (conditional)
        _entities.Add(new StorageFile(storage, path));
    return this;
}

public IStorageEntity Files(FileInfo[] paths)
{
    var en = _entities;
    var st = storage;
    paths.Pipe(x => en.Add(new StorageFile(st, x))).Consume();
    return this;
}

public IStorageEntity Folder(string folderName, Action<IStorageEntity> actor)
{
    var entity = new StorageFolder(storage, folderName);
    _entities.Add(entity);
    actor(entity);
    return this;
}
public ShardBuilder Return()
    => storage._builder;

public IEnumerable<IStorageEntity> InnerEntities
    => _entities;
}

public record struct StorageFile(ShardFileStorage storage, FileInfo info) : IStorageEntity
{
        public string Name => info.Name;

public IStorageEntity File(FileInfo path)
    => throw new NotSupportedException("File entity cannot create inner file.");
public IStorageEntity File(FileInfo path, bool conditional)
    => throw new NotSupportedException("File entity cannot create inner file.");
public IStorageEntity Files(FileInfo[] path)
    => throw new NotSupportedException("File entity cannot create inner file.");
public IStorageEntity Folder(string folderName, Action<IStorageEntity> actor)
    => throw new NotSupportedException("File entity cannot create inner folder.");

public IEnumerable<IStorageEntity> InnerEntities
    => throw new NotSupportedException("File entity cannot contains inner entities.");

public ShardBuilder Return() => storage._builder;
}

public interface IStorageEntity
{
    string Name { get; }

    IStorageEntity File(FileInfo path);
    IStorageEntity File(FileInfo path, bool conditional);
    IStorageEntity Files(FileInfo[] paths);
    IStorageEntity Folder(string folderName, Action<IStorageEntity> actor);

    IEnumerable<IStorageEntity> InnerEntities { get; }

    ShardBuilder Return();
}
