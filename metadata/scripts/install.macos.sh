#!/bin/bash

Architecture=$(uname -m)
case "$Architecture" in
    "x86_64")
        OS="macos-x64"
        ;;
    "arm64")
        OS="macos-arm64"
        ;;
    *)
        echo "Unknown Arch: $Architecture."
        exit 1
        ;;
esac

DownloadUrl="https://github.com/vein-lang/vein/releases/download/v0.12/$OS-build.zip"
InstallPath="$HOME/.vein"

if [ -d "$InstallPath" ]; then
    echo "Uninstall $InstallPath"
    rm -rf "$InstallPath"/*
else
    mkdir -p "$InstallPath"
fi

ZipPath="/tmp/vein-tools.zip"
echo "Downloading $DownloadUrl into $ZipPath"
curl -L -o "$ZipPath" "$DownloadUrl"

echo "Unzipping into $InstallPath"
unzip -q "$ZipPath" -d "$InstallPath"

ProfilePath="$HOME/.zshrc"
if [ -f "$HOME/.bash_profile" ]; then
    ProfilePath="$HOME/.bash_profile"
elif [ -f "$HOME/.zshrc" ]; then
    ProfilePath="$HOME/.zshrc"
elif [ -f "$HOME/.bashrc" ]; then
    ProfilePath="$HOME/.bashrc"
else
    echo "bad shell profile"
    exit 1
fi

PathEntry="export PATH=\$PATH:$InstallPath"
if ! grep -Fxq "$PathEntry" "$ProfilePath"; then
    echo "Add $InstallPath into PATH"
    echo "$PathEntry" >> "$ProfilePath"
    echo "Changed $ProfilePath. Please reload profile 'source $ProfilePath'."
else
    echo "$InstallPath already in PATH"
fi

echo "Install complete"