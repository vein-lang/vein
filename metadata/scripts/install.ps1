$arch = if ([System.Environment]::Is64BitOperatingSystem) { "win-x64" } else { "win-arm64" }

$apiUrl = "https://releases.vein-lang.org/api/get-release"

$outputDir = "$HOME\.vein"

function DownloadFile($url, $outputFile) {
    $webClient = New-Object System.Net.WebClient
    $webClient.DownloadFile($url, $outputFile)
}

try {
    $releaseInfo = Invoke-RestMethod -Uri $apiUrl
    $asset = $releaseInfo.assets | Where-Object { $_.name -eq "rune.$arch.zip" }
    $downloadUrl = $asset.browser_download_url
    Write-Output $downloadUrl
    New-Item -ItemType Directory -Force -Path $outputDir > $null
    $zipFile = "$outputDir\rune.$arch.zip"
    DownloadFile $downloadUrl $zipFile
    Expand-Archive -Path $zipFile -DestinationPath $outputDir -Force > $null
    Remove-Item -Force $zipFile > $null
    $env:Path += ";$outputDir"
    [Environment]::SetEnvironmentVariable("Path", $env:Path, [EnvironmentVariableTarget]::User)

    Invoke-Expression "$outputDir\rune.exe workload update vein.runtime@0.30.2"
    Invoke-Expression "$outputDir\rune.exe workload update vein.compiler@0.30.2"
    Write-Output "Rune Installed, restart your teminal for use"
}
catch {
    Write-Error "Failed to install Vein Rune package: $_"
}