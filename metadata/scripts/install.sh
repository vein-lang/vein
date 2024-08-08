#!/bin/bash

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
BIN_DIR="$OUTPUT_DIR/bin"

download_file() {
    url="$1"
    output_file="$2"
    curl -L -o "$output_file" "$url"
}
mkdir -p "$OUTPUT_DIR"
mkdir -p "$BIN_DIR"

ZIP_FILE="$OUTPUT_DIR/rune.$RUNTIME.zip"
download_file "$download_url" "$ZIP_FILE"

unzip -o "$ZIP_FILE" -d "$BIN_DIR"

rm "$ZIP_FILE"

if [[ ":$PATH:" != *":$BIN_DIR:"* ]]; then
    echo "export PATH=\$PATH:$BIN_DIR" >> "$HOME/.bashrc"
    source "$HOME/.bashrc"
fi

if [ -f "$HOME/.zshrc" ]; then
    echo "export PATH=\$PATH:$BIN_DIR" >> "$HOME/.zshrc"
    source "$HOME/.zshrc"
fi

"$BIN_DIR/rune" install workload vein.runtime --version latest
"$BIN_DIR/rune" install workload vein.compiler --version latest

echo "Installation complete. Please restart your terminal to use the new PATH."