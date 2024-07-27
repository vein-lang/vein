if ($env:PROCESSOR_ARCHITECTURE -eq "AMD64") {
    $OS = "win-x64"
} elseif ($env:PROCESSOR_ARCHITECTURE -eq "ARM64") {
    $OS = "win-arm64"
} else {
    Write-Host "Unknown Arch: $env:PROCESSOR_ARCHITECTURE."
    return
}

$DownloadUrl = "https://github.com/vein-lang/vein/releases/download/v0.12/$OS-build.zip"
$InstallPath = "$HOME\.vein"

if (Test-Path -Path $InstallPath) {
    Write-Host "Uninstall $InstallPath"
    Remove-Item -Path $InstallPath\* -Recurse -Force
} else {
    New-Item -Path $InstallPath -ItemType Directory
}

$ZipPath = "$env:TEMP\vein-tools.zip"
Write-Host "Downloading $DownloadUrl into $ZipPath"
Invoke-WebRequest -Uri $DownloadUrl -OutFile $ZipPath

Write-Host "Unzipping $InstallPath"
Expand-Archive -Path $ZipPath -DestinationPath $InstallPath

$CurrentPath = [System.Environment]::GetEnvironmentVariable("PATH", "User")
if ($CurrentPath -notcontains $InstallPath) {
    Write-Host "Add $InstallPath into PATH"
    [System.Environment]::SetEnvironmentVariable("PATH", "$CurrentPath;$InstallPath", "User")
}

Write-Host "Install complete, please refresh env before use commands"