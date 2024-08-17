$arch = if ([System.Environment]::Is64BitOperatingSystem) { "win-x64" } else { "win-arm64" }

$apiUrl = "https://releases.vein-lang.org/api/get-release"

$outputDir = "$HOME\.vein"
$binDir = "$outputDir\bin"

function DownloadFile($url, $outputFile) {
    $webClient = New-Object System.Net.WebClient
    $webClient.DownloadFile($url, $outputFile)
}

try {
    $releaseInfo = Invoke-RestMethod -Uri $apiUrl
    $asset = $releaseInfo.assets | Where-Object { $_.name -eq "rune.$arch.zip" }
    $downloadUrl = $asset.browser_download_url
    New-Item -ItemType Directory -Force -Path $outputDir > $null
    New-Item -ItemType Directory -Force -Path $binDir > $null
    $zipFile = "$outputDir\rune.$arch.zip"
    DownloadFile $downloadUrl $zipFile
    Expand-Archive -Path $zipFile -DestinationPath $binDir -Force > $null
    Remove-Item -Force $zipFile > $null
    $env:Path += ";$binDir"
    [Environment]::SetEnvironmentVariable("Path", $env:Path, [EnvironmentVariableTarget]::User)

    & "$binDir\rune.exe" install workload vein.runtime --version latest
    & "$binDir\rune.exe" install workload vein.compiler --version latest
    

    Write-Output "Rune Installed, restart your teminal for use"
}
catch {
    Write-Error "Failed to install Vein Rune package: $_"
}