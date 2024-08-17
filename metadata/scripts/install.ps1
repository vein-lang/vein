$arch = if ([System.Environment]::Is64BitOperatingSystem) { "win-x64" } else { "win-arm64" }

if (([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")) {
    Write-Host "You cannot install vein-sdk from under the administrator." -ForegroundColor Red
    exit 1
} 

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
    $currentPath = [Environment]::GetEnvironmentVariable("Path", [EnvironmentVariableTarget]::User)
    if (-not $currentPath.Contains("$outputDir\bin")) {
        $env:Path += ";$outputDir\bin"
        [Environment]::SetEnvironmentVariable("Path", $env:Path, [EnvironmentVariableTarget]::User)
    }

    $installWorkloads = Read-Host "Do you want to install vein.runtime and vein.compiler workloads? (y/n)"
    if ($installWorkloads -eq 'y') {
        Invoke-Expression "$outputDir\rune.exe workload install vein.runtime@0.30.3"
        Invoke-Expression "$outputDir\rune.exe workload install vein.compiler@0.30.3"
        Write-Output "Workloads installed."
    } else {
        Write-Output "Workloads installation skipped."
    }
    Write-Output "Rune Installed, restart your teminal for use"
}
catch {
    Write-Error "Failed to install Vein Rune package: $_"
}