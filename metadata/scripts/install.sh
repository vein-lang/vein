#!/bin/bash

if [ "$(id -u)" -eq 0 ]; then
    echo "You cannot install vein-sdk from under the root." >&2
    exit 1
fi

for cmd in unzip jq curl; do
  if ! command -v $cmd &> /dev/null; then
    echo "Error: $cmd is not installed or not available in PATH."
    exit 1
  fi
done

OS="$(uname -s)"
ARCH="$(uname -m)"
RUNTIME=""

if [[ "$OS" == "Linux" ]]; then
    if [[ "$ARCH" == "x86_64" ]]; then
        RUNTIME="linux-x64"
    elif [[ "$ARCH" == "aarch64" ]]; then
        RUNTIME="linux-arm64"
    else
        echo "Unsupported architecture: $ARCH"
        exit 1
    fi
elif [[ "$OS" == "Darwin" ]]; then
    if [[ "$ARCH" == "x86_64" ]]; then
        RUNTIME="osx-x64"
    elif [[ "$ARCH" == "arm64" ]]; then
        RUNTIME="osx-arm64"
    else
        echo "Unsupported architecture: $ARCH"
        exit 1
    fi
else
    echo "Unsupported OS: $OS"
    exit 1
fi

API_URL="https://releases.vein-lang.org/api/get-release"
release_info=$(curl -s "$API_URL")
download_url=$(echo "$release_info" | jq -r ".assets[] | select(.name == \"rune.$RUNTIME.zip\") | .browser_download_url")
OUTPUT_DIR="$HOME/.vein"

download_file() {
    url="$1"
    output_file="$2"
    curl -L -o "$output_file" "$url"
}

mkdir -p "$OUTPUT_DIR"

ZIP_FILE="$OUTPUT_DIR/rune.$RUNTIME.zip"
download_file "$download_url" "$ZIP_FILE"

unzip -o "$ZIP_FILE" -d "$OUTPUT_DIR"
rm "$ZIP_FILE"

if [[ ":$PATH:" != *":$OUTPUT_DIR/bin:"* ]]; then
    echo "export PATH=\$PATH:$OUTPUT_DIR/bin" >> "$HOME/.bashrc"
    if [ -f "$HOME/.zshrc" ]; then
        echo "export PATH=\$PATH:$OUTPUT_DIR/bin" >> "$HOME/.zshrc"
    fi
    source "$HOME/.bashrc"
    source "$HOME/.zshrc" 2>/dev/null
fi

chmod +x "$OUTPUT_DIR/rune"
chmod +x "$OUTPUT_DIR/bin/rune.sh"

RUNE_NOVID=1 "$OUTPUT_DIR/rune" telemetry

read -p "Do you want to install vein.runtime and vein.compiler workloads? (y/n): " install_workloads
if [[ "$install_workloads" == "y" ]]; then
    RUNE_NOVID=1 "$OUTPUT_DIR/rune" workload install vein.runtime@0.30.3
    RUNE_NOVID=1 "$OUTPUT_DIR/rune" workload install vein.compiler@0.30.3
    echo "Workloads installed."
else
    echo "Workloads installation skipped."
fi

echo "Installation complete. Please restart your terminal to use the new PATH."