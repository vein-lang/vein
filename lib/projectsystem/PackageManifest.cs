namespace vein.project;

using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using NuGet.Versioning;

public record PackageManifest
{
    [JsonProperty("name")]
    public string Name { get; set; }
    [JsonProperty("version"), JsonConverter(typeof(NuGetVersionConverter))]
    public NuGetVersion Version { get; set; }
    [JsonProperty("description")]
    public string Description { get; set; }
    [JsonProperty("authors")]
    public List<string> Authors { get; set; } = new();
    [JsonProperty("icon")]
    public string Icon { get; set; }
    [JsonProperty("license")]
    public string License { get; set; }
    [JsonProperty("preview")]
    public bool IsPreview { get; set; }
    [JsonProperty("bugs")]
    public Uri BugUrl { get; set; }
    [JsonProperty("homepage")]
    public Uri HomepageUrl { get; set; }
    [JsonProperty("repository")]
    public Uri Repository { get; set; }
    [JsonProperty("keywords")]
    public List<string> Keywords { get; set; } = new();
    [JsonProperty("categories")]
    public List<string> Categories { get; set; } = new();
    [JsonProperty("dependencies")]
    public List<PackageReference> Dependencies { get; set; } = new();
    [JsonProperty("requireLicenseAcceptance")]
    public bool RequireLicenseAcceptance { get; set; }
}

public record PackageMetadata
{
    [JsonProperty("publish_date")]
    public DateTimeOffset PublishDate { get; set; }
    [JsonProperty("id")]
    public string ID { get; set; }
}

public record PackageCertificate([JsonProperty("cert")] string PublicCertificate) { }

