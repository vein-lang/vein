param(
    [ValidateSet("Debug", "Release", "RelWithDebInfo", "MinSizeRel")]
    [string]$Configuration = "Release",

    [ValidateSet("x64", "ARM64")]
    [string]$Arch = "x64",

    [string]$SourceDir = "$PSScriptRoot/runtime/libuv",
    [string]$BuildDirShared = "$PSScriptRoot/runtime/libuv/.build/$Arch/shared",
    [string]$BuildDirStatic = "$PSScriptRoot/runtime/libuv/.build/$Arch/static",
    [string]$OutputDir = "$PSScriptRoot/runtime/libuv/.output/$Arch"
)

$ErrorActionPreference = "Stop"

# Verify cmake is available
if (-not (Get-Command cmake -ErrorAction SilentlyContinue)) {
    Write-Error "cmake not found in PATH. Install CMake and ensure it's on your PATH."
    exit 1
}

# Clean and create build directories
foreach ($dir in @($BuildDirShared, $BuildDirStatic)) {
    if (Test-Path $dir) {
        Remove-Item $dir -Recurse -Force
    }
    New-Item -ItemType Directory -Path $dir -Force | Out-Null
}

# Clean and create output directory
if (Test-Path $OutputDir) {
    Remove-Item $OutputDir -Recurse -Force
}
New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null

# Find VS and import MSVC environment
$vsWhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
if (-not (Test-Path $vsWhere)) {
    $vsWhere = "${env:ProgramFiles}\Microsoft Visual Studio\Installer\vswhere.exe"
}

$vsInstallPath = $null
if (Test-Path $vsWhere) {
    $vsInstallPath = & $vsWhere -latest -property installationPath
}

if ($vsInstallPath) {
    $vcvarsArch = switch ($Arch) {
        "x64"   { "amd64" }
        "ARM64" { "amd64_arm64" }
    }
    $vcvarsall = Join-Path $vsInstallPath "VC\Auxiliary\Build\vcvarsall.bat"
    Write-Host "  Using VS: $vsInstallPath" -ForegroundColor DarkGray
    cmd /c "`"$vcvarsall`" $vcvarsArch >nul 2>&1 && set" | ForEach-Object {
        if ($_ -match '^([^=]+)=(.*)$') {
            [System.Environment]::SetEnvironmentVariable($matches[1], $matches[2], "Process")
        }
    }
    Write-Host "  MSVC environment loaded for $vcvarsArch" -ForegroundColor DarkGray
} else {
    Write-Host "  vswhere not found, assuming compiler is already on PATH" -ForegroundColor DarkYellow
}

Write-Host "=== Building libuv ===" -ForegroundColor Cyan
Write-Host "  Configuration: $Configuration"
Write-Host "  Architecture:  $Arch"
Write-Host "  Source:        $SourceDir"
Write-Host "  Build shared:  $BuildDirShared"
Write-Host "  Build static:  $BuildDirStatic"
Write-Host "  Output:        $OutputDir"
Write-Host ""

# Common CMake flags
$commonArgs = @(
    "-S", $SourceDir,
    "-G", "Ninja",
    "-DCMAKE_BUILD_TYPE=$Configuration",
    "-DLIBUV_BUILD_TESTS=OFF",
    "-DLIBUV_BUILD_BENCH=OFF"
)

# ============================================================
# Build shared library (DLL) for runtime P/Invoke
# ============================================================
Write-Host "--- [Shared] CMake Configure ---" -ForegroundColor Yellow
$sharedArgs = $commonArgs + @("-B", $BuildDirShared, "-DLIBUV_BUILD_SHARED=ON")
cmake @sharedArgs
if ($LASTEXITCODE -ne 0) {
    Write-Error "CMake configure (shared) failed with exit code $LASTEXITCODE"
    exit $LASTEXITCODE
}

Write-Host ""
Write-Host "--- [Shared] CMake Build ---" -ForegroundColor Yellow
cmake --build $BuildDirShared --parallel
if ($LASTEXITCODE -ne 0) {
    Write-Error "CMake build (shared) failed with exit code $LASTEXITCODE"
    exit $LASTEXITCODE
}

# ============================================================
# Build static library (.lib) for AOT linking
# ============================================================
Write-Host ""
Write-Host "--- [Static] CMake Configure ---" -ForegroundColor Yellow
$staticArgs = $commonArgs + @("-B", $BuildDirStatic, "-DLIBUV_BUILD_SHARED=OFF")
cmake @staticArgs
if ($LASTEXITCODE -ne 0) {
    Write-Error "CMake configure (static) failed with exit code $LASTEXITCODE"
    exit $LASTEXITCODE
}

Write-Host ""
Write-Host "--- [Static] CMake Build ---" -ForegroundColor Yellow
cmake --build $BuildDirStatic --parallel
if ($LASTEXITCODE -ne 0) {
    Write-Error "CMake build (static) failed with exit code $LASTEXITCODE"
    exit $LASTEXITCODE
}

# ============================================================
# Collect outputs
# ============================================================
Write-Host ""
Write-Host "--- Collecting outputs ---" -ForegroundColor Yellow

# Shared: uv.dll -> libuv.dll (P/Invoke name is "libuv")
$dllSource = Get-ChildItem -Path $BuildDirShared -Recurse -Filter "uv.dll" | Select-Object -First 1
$impLibSource = Get-ChildItem -Path $BuildDirShared -Recurse -Filter "uv.lib" | Select-Object -First 1

if ($dllSource) {
    Copy-Item $dllSource.FullName -Destination "$OutputDir/libuv.dll" -Force
    Write-Host "  [shared] $($dllSource.FullName) -> $OutputDir/libuv.dll" -ForegroundColor Green
} else {
    Write-Warning "uv.dll not found in shared build output"
}

if ($impLibSource) {
    Copy-Item $impLibSource.FullName -Destination "$OutputDir/libuv.import.lib" -Force
    Write-Host "  [shared] $($impLibSource.FullName) -> $OutputDir/libuv.import.lib" -ForegroundColor Green
}

# Static: libuv.lib (already named correctly due to PREFIX "lib" + OUTPUT_NAME "uv")
$staticLibSource = Get-ChildItem -Path $BuildDirStatic -Recurse -Filter "libuv.lib" | Select-Object -First 1

if ($staticLibSource) {
    Copy-Item $staticLibSource.FullName -Destination "$OutputDir/libuv-static.lib" -Force
    Write-Host "  [static] $($staticLibSource.FullName) -> $OutputDir/libuv-static.lib" -ForegroundColor Green
} else {
    Write-Warning "libuv.lib not found in static build output"
}

Write-Host ""
Write-Host "=== Build complete ===" -ForegroundColor Cyan
Write-Host "  Output: $OutputDir" -ForegroundColor Green
Write-Host "    libuv.dll          - shared library (runtime P/Invoke)" -ForegroundColor Green
Write-Host "    libuv.import.lib   - import library for shared DLL" -ForegroundColor Green
Write-Host "    libuv-static.lib   - static library (AOT linking)" -ForegroundColor Green
