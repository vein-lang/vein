param(
    [ValidateSet("Debug", "Release", "RelWithDebInfo", "MinSizeRel")]
    [string]$Configuration = "Release",

    [ValidateSet("x64", "ARM64")]
    [string]$Arch = "x64",

    [string]$SourceDir = "$PSScriptRoot/runtime/bdwgc",
    [string]$BuildDirShared = "$PSScriptRoot/runtime/bdwgc/.build/$Arch/shared",
    [string]$BuildDirStatic = "$PSScriptRoot/runtime/bdwgc/.build/$Arch/static",
    [string]$OutputDir = "$PSScriptRoot/runtime/bdwgc/.output/$Arch"
)

$ErrorActionPreference = "Stop"

# Verify cmake is available
if (-not (Get-Command cmake -ErrorAction SilentlyContinue)) {
    Write-Error "cmake not found in PATH. Install CMake and ensure it's on your PATH."
    exit 1
}

# Ensure libatomic_ops is present (required by bdwgc on MSVC)
$libatomicOpsDir = Join-Path $SourceDir "libatomic_ops"
if (-not (Test-Path (Join-Path $libatomicOpsDir "src\atomic_ops.h"))) {
    Write-Host "--- Fetching libatomic_ops ---" -ForegroundColor Yellow
    if (Test-Path $libatomicOpsDir) {
        Remove-Item $libatomicOpsDir -Recurse -Force
    }
    git clone --depth 1 https://github.com/ivmai/libatomic_ops.git $libatomicOpsDir
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Failed to clone libatomic_ops"
        exit 1
    }
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

# Determine CMake architecture flag for Ninja
# For Ninja we set the target arch via CMAKE_SYSTEM_PROCESSOR and toolchain
# We need to run from a VS Developer shell or find vcvarsall
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
    # Import MSVC environment into current PowerShell session
    $envBefore = @{}
    Get-ChildItem env: | ForEach-Object { $envBefore[$_.Name] = $_.Value }
    cmd /c "`"$vcvarsall`" $vcvarsArch >nul 2>&1 && set" | ForEach-Object {
        if ($_ -match '^([^=]+)=(.*)$') {
            [System.Environment]::SetEnvironmentVariable($matches[1], $matches[2], "Process")
        }
    }
    Write-Host "  MSVC environment loaded for $vcvarsArch" -ForegroundColor DarkGray
} else {
    Write-Host "  vswhere not found, assuming compiler is already on PATH" -ForegroundColor DarkYellow
}

Write-Host "=== Building bdwgc (Boehm GC) ===" -ForegroundColor Cyan
Write-Host "  Configuration: $Configuration"
Write-Host "  Architecture:  $Arch"
Write-Host "  Source:        $SourceDir"
Write-Host "  Build shared:  $BuildDirShared"
Write-Host "  Build static:  $BuildDirStatic"
Write-Host "  Output:        $OutputDir"
Write-Host ""

# Common CMake flags based on what IshtarGC/BoehmGCLayout.cs requires
$commonArgs = @(
    "-S", $SourceDir,
    "-G", "Ninja",
    "-DCMAKE_BUILD_TYPE=$Configuration",
    "-Dbuild_cord=OFF",
    "-Dbuild_tests=OFF",
    "-Denable_docs=OFF",
    "-Denable_cplusplus=OFF",
    "-Denable_threads=ON",
    "-Denable_parallel_mark=ON",
    "-Denable_thread_local_alloc=ON",
    "-Denable_threads_discovery=ON",
    "-Denable_gcj_support=OFF",
    "-Denable_java_finalization=ON",
    "-Denable_atomic_uncollectable=ON",
    "-Denable_disclaim=ON",
    "-Denable_large_config=OFF",
    "-Denable_gc_debug=OFF",
    "-Denable_redirect_malloc=OFF",
    "-Denable_munmap=ON",
    "-Denable_dynamic_loading=ON",
    "-Denable_register_main_static_data=ON",
    "-Denable_handle_fork=OFF",
    "-Dinstall_headers=OFF",
    "-Denable_gc_assertions=OFF"
)

# ============================================================
# Build shared library (DLL) for runtime P/Invoke
# ============================================================
Write-Host "--- [Shared] CMake Configure ---" -ForegroundColor Yellow
$sharedArgs = $commonArgs + @("-B", $BuildDirShared, "-DBUILD_SHARED_LIBS=ON", "-DCMAKE_INSTALL_PREFIX=$OutputDir/shared")
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
# Build static library (.lib) for AOT linking into VM
# ============================================================
Write-Host ""
Write-Host "--- [Static] CMake Configure ---" -ForegroundColor Yellow
$staticArgs = $commonArgs + @("-B", $BuildDirStatic, "-DBUILD_SHARED_LIBS=OFF", "-DCMAKE_INSTALL_PREFIX=$OutputDir/static")
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

# Shared: gc.dll -> libgc.dll (P/Invoke name)
$dllSource = Get-ChildItem -Path $BuildDirShared -Recurse -Filter "gc.dll" | Select-Object -First 1
$impLibSource = Get-ChildItem -Path $BuildDirShared -Recurse -Filter "gc.lib" | Select-Object -First 1

if ($dllSource) {
    Copy-Item $dllSource.FullName -Destination "$OutputDir/libgc.dll" -Force
    Write-Host "  [shared] $($dllSource.FullName) -> $OutputDir/libgc.dll" -ForegroundColor Green
} else {
    Write-Warning "gc.dll not found in shared build output"
}

if ($impLibSource) {
    Copy-Item $impLibSource.FullName -Destination "$OutputDir/libgc.import.lib" -Force
    Write-Host "  [shared] $($impLibSource.FullName) -> $OutputDir/libgc.import.lib" -ForegroundColor Green
}

# Static: gc.lib -> libgc-static.lib (for AOT linking)
$staticLibSource = Get-ChildItem -Path $BuildDirStatic -Recurse -Filter "gc.lib" | Select-Object -First 1

if ($staticLibSource) {
    Copy-Item $staticLibSource.FullName -Destination "$OutputDir/libgc-static.lib" -Force
    Write-Host "  [static] $($staticLibSource.FullName) -> $OutputDir/libgc-static.lib" -ForegroundColor Green
} else {
    Write-Warning "gc.lib not found in static build output"
}

Write-Host ""
Write-Host "=== Build complete ===" -ForegroundColor Cyan
Write-Host "  Output: $OutputDir" -ForegroundColor Green
Write-Host "    libgc.dll          - shared library (runtime P/Invoke)" -ForegroundColor Green
Write-Host "    libgc.import.lib   - import library for shared DLL" -ForegroundColor Green
Write-Host "    libgc-static.lib   - static library (AOT linking)" -ForegroundColor Green
