#!/bin/bash

Architecture=$(uname -m)
case "$Architecture" in
    "x86_64")
        OS="linux-x64"
        ;;
    "aarch64")
        OS="linux-arm64"
        ;;
    "armv7l" | "armv6l")
        OS="linux-arm32"
        ;;
    *)
        echo "Unknown arch: $Architecture."
        exit 1
        ;;
esac

DownloadUrl="https://github.com/vein-lang/vein/releases/download/v0.12/$OS-build.zip"
InstallPath="$HOME/.vein"

if [ -d "$InstallPath" ]; then
    echo "Uninstall legacy version from $InstallPath"
    rm -rf "$InstallPath"/*
else
    mkdir -p "$InstallPath"
fi

ZipPath="/tmp/vein-tools.zip"
echo "Downloading $DownloadUrl into $ZipPath"
curl -L -o "$ZipPath" "$DownloadUrl"

echo "Unzipping into $InstallPath"
unzip -q "$ZipPath" -d "$InstallPath"

ProfilePath="$HOME/.profile"
PathEntry="export PATH=\$PATH:$InstallPath"

if ! grep -Fxq "$PathEntry" "$ProfilePath"; then
    echo "Set $InstallPath into PATH"
    echo "$PathEntry" >> "$ProfilePath"
    echo "Changed $ProfilePath. Please reload profile 'source $ProfilePath'."
else
    echo "$InstallPath already in PATH"
fi

echo "Установка завершена."