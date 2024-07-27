(new-object Net.WebClient).DownloadString("https://raw.githubusercontent.com/filipw/dotnet-script/master/install/install.ps1") | iex
dotnet script ./download.blobs.csx
dotnet script ./pack.blobs.csx