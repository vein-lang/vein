#!/usr/bin/env dotnet-script
#load "includes.csx"

using System.Net;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

using Flurl;
using Flurl.Http;
using Spectre.Console;
var token = Environment.GetEnvironmentVariable("GITHUB_PAT");


while (token is null)
    token = AnsiConsole.Ask<string>("Github Token: ", null);


const long workflow_id = 10554241;
const string user_agent = "github.com/vein-lang/vein";

token = $"Bearer {token}";


try
{
    var user = await $"https://api.github.com/user"
        .WithHeader("Authorization", token)
        .WithHeader("User-Agent", user_agent)
        .GetJsonAsync<Github.User>();
    AnsiConsole.MarkupLine($"Hello [green]{user.name}[/] [gray]@{user.login}[/]");
}
catch (Exception e)
{
    AnsiConsole.MarkupLine($"[red]Error[/] validate token.");
    AnsiConsole.WriteException(e);
    return -1;
}

var result = await "https://api.github.com/repos/vein-lang/vein/actions/runs"
    .WithHeader("Authorization", token)
    .WithHeader("User-Agent", user_agent)
    .GetJsonAsync<Github.Workflow.Runs>();
foreach (var item in result.workflow_runs)
{
    if (item.workflow_id != workflow_id)
        continue;
    if (item.conclusion != "success")
        continue;
    var run_id = item.id;
    var artifacts = await $"https://api.github.com/repos/vein-lang/vein/actions/runs/{run_id}/artifacts"
        .WithHeader("Authorization", token)
        .WithHeader("User-Agent", user_agent)
        .GetJsonAsync<Github.Workflow.Artifacts>();

    await AnsiConsole.Progress().Columns(new ProgressColumn[]
    {
        new TaskDescriptionColumn() { Alignment = Justify.Left },
        new ProgressBarColumn(),
        new PercentageColumn(),
        new RemainingTimeColumn(),
        new SpinnerColumn(),
    }).StartAsync(async context =>
    {
        foreach (var artifact in artifacts.artifacts)
        {
            if (artifact.expired)
            {
                Console.WriteLine($"[red]Error[/]: Latest artifact [gray]'{artifact.name}::{artifact.id}'[/] has [red]expired[/].");
                throw new Exception();
            }

        

            await $"{artifact.archive_download_url}"
                .WithHeader("Authorization", token)
                .WithHeader("User-Agent", user_agent)
                .DownloadFileAsync(@"C:\git\vein-lang\installer\gen\blobs");


            var downloader = context.AddTask($"📀 Downloading [gray]'{artifact.name}'[/]...");

            using (var client = new WebClient())
            {
                client.Headers.Add("Authorization", token);
                client.Headers.Add("User-Agent", user_agent);
                client.DownloadProgressChanged += (sender, a) =>
                {
                    if (a.TotalBytesToReceive == -1)
                        downloader.IsIndeterminate();
                    else
                        downloader.MaxValue(a.TotalBytesToReceive).Value(a.BytesReceived);
                };
                await client.DownloadFileTaskAsync(artifact.archive_download_url,
                    @".\blobs\" + artifact.name + ".zip");
            }
        }
    });

    return 0;
}


public class Github
{
    public class User
    {
        public string id;
        public string login;
        public string name;
    }
    public class Workflow
    {
        public class Runs
        {
            public Run[] workflow_runs = null;

        }
        public class Run
        {
            public int id;
            public string name;
            public string node_id;
            public string head_branch;
            public long workflow_id;
            public string conclusion;
        }

        public class Artifacts
        {
            public Artifact[] artifacts;
        }

        public class Artifact
        {
            public int id;
            public string name;
            public string node_id;
            public string archive_download_url;
            public bool expired;
        }
    }
}
