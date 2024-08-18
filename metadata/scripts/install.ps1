$arch = if ([System.Environment]::Is64BitOperatingSystem) { "win-x64" } else { "win-arm64" }

if (([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")) {
    Write-Host "You cannot install vein-sdk from under the administrator." -ForegroundColor Red
    exit 1
} 
$env:RUNE_NOVID = "1";
$env:VEINC_NOVID = "1";

$apiUrl = "https://releases.vein-lang.org/api/get-release"

$outputDir = "$HOME\.vein"

function DownloadFile {
    param (
        [string]$url,
        [string]$outputFile
    )

    $fileStream = $null
    $stream = $null

    try {
        $request = [System.Net.HttpWebRequest]::Create($url)
        $request.Method = "GET"
        $response = $request.GetResponse()
        $totalBytes = $response.ContentLength
        $stream = $response.GetResponseStream()
        $fileStream = [System.IO.File]::Create($outputFile)
        $buffer = New-Object byte[] 8192
        $totalReadBytes = 0
        while ($true) {
            $readBytes = $stream.Read($buffer, 0, $buffer.Length)
            if ($readBytes -le 0) { break }
            $fileStream.Write($buffer, 0, $readBytes)
            $totalReadBytes += $readBytes
            $percentComplete = [math]::Round(($totalReadBytes / $totalBytes) * 100, 2)
            Write-Progress -Activity "Downloading $url" -Status "$percentComplete% Complete" -PercentComplete $percentComplete
        }
    } finally {
        if ($fileStream) {
            $fileStream.Close()
        }
        if ($stream) {
            $stream.Close()
        }
        Write-Progress -Activity "Downloading $url" -Status "Download Complete" -Completed
    }
}


try {
    $releaseInfo = Invoke-RestMethod -Uri $apiUrl
    $asset = $releaseInfo.assets | Where-Object { $_.name -eq "rune.$arch.zip" }
    $downloadUrl = $asset.browser_download_url
    $tagVersion = $releaseInfo.tag_name.Replace("v", "")
    Write-Host $tagVersion
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

    Invoke-Expression "$outputDir\rune.exe telemetry"

    if ($LASTEXITCODE -ne 0) {
        exit $LASTEXITCODE
    }

    $installWorkloads = Read-Host "Do you want to install vein.runtime and vein.compiler workloads? (y/n)"
    if ($installWorkloads -eq 'y') {
        Invoke-Expression "$outputDir\rune.exe workload install vein.runtime@$tagVersion"
        if ($LASTEXITCODE -ne 0) {
            exit $LASTEXITCODE
        }
        Invoke-Expression "$outputDir\rune.exe workload install vein.compiler@$tagVersion"
        if ($LASTEXITCODE -ne 0) {
            exit $LASTEXITCODE
        }
        Write-Output "Workloads installed."
    } else {
        Write-Output "Workloads installation skipped."
    }
    Write-Output "Rune Installed, restart your teminal for use"
}
catch {
    Write-Error "Failed to install Vein Rune package: $_"
}