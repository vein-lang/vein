#!/bin/bash

if [ "$(id -u)" -eq 0 ]; then
    echo "You cannot install vein-sdk from under the root." >&2
    exit 1
fi

for cmd in unzip grep jq curl; do
  if ! command -v $cmd &> /dev/null; then
    echo "Error: $cmd is not installed or not available in PATH."
    exit 1
  fi
done

version_ge() {
  [ "$(printf '%s\n' "$1" "$2" | sort -V | head -n1)" == "$2" ]
}

if [[ "$(uname)" == "Linux" ]]; then
  kernel_version=$(uname -r | cut -d- -f1)
  echo "Linux kernel version: $kernel_version"
  required_kernel_version="6.1"
  if ! version_ge "$kernel_version" "$required_kernel_version"; then
    echo "Error: Linux kernel version must be 6.1 or higher. Current version: $kernel_version"
    exit 1
  fi

elif [[ "$(uname)" == "Darwin" ]]; then
  macos_version=$(sw_vers -productVersion)
  echo "macOS version: $macos_version"
  required_macos_version="14.0"
  if ! version_ge "$macos_version" "$required_macos_version"; then
    echo "Error: macOS version must be 14.0 or higher. Current version: $macos_version"
    exit 1
  fi
else
  echo "Unsupported OS: $(uname)"
  exit 1
fi


grep_version=$(grep --version | head -n1 | awk '{print $NF}')
required_version="3.7"

if ! version_ge "$grep_version" "$required_version"; then
  echo "Error: grep version must be 3.7 or higher. Current version: $grep_version"
  exit 1
fi

OS="$(uname -s)"
ARCH="$(uname -m)"
RUNTIME=""
export RUNE_NOVID=1
export VEINC_NOVID=1
apiUrl="https://releases.vein-lang.org/api/get-release"
outputDir="$HOME/.vein"

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
        echo "Error: x86_64 is not supported"
        exit 1
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

download_file() {
  url=$1
  outputFile=$2

  curl -L --progress-bar -o "$outputFile" "$url"
}

echo "Fetching release information..."
releaseInfo=$(curl -s $apiUrl)
echo $RUNTIME
downloadUrl=$(echo $releaseInfo | grep -oP "\"browser_download_url\":\s*\"[^\"]*rune.$RUNTIME.zip\"" | cut -d\" -f4)
tagVersion=$(echo $releaseInfo | grep -oP "\"tag_name\":\s*\"v[^\"]*\"" | cut -d\" -f4 | sed 's/v//')

echo "Version: $tagVersion"

mkdir -p "$outputDir"
zipFile="$outputDir/rune.$RUNTIME.zip"

echo "Downloading Rune SDK... ($downloadUrl) -> ($zipFile)"
download_file "$downloadUrl" "$zipFile"


echo "Extracting..."
unzip -o "$zipFile" -d "$outputDir" > /dev/null
rm "$zipFile"


chmod +x "$outputDir/bin/rune.sh"
chmod +x "$outputDir/rune"

if [[ "$OS" == "Darwin" ]]; then
    echo "spctl --add '$outputDir/rune'"
    spctl --add "$outputDir/rune"

    echo "codesign --sign - --force --deep '$outputDir/rune'"
    sudo codesign --sign - --force --deep "$outputDir/rune"

    echo "xattr -dr com.apple.quarantine '$outputDir/rune'"
    xattr -dr com.apple.quarantine $outputDir/rune
fi

"$outputDir/rune" telemetry

if [ $? -ne 0 ]; then
  exit 1
fi

read -p "Do you want to install vein.runtime and vein.compiler workloads? y/n " installWorkloads

if [ "$installWorkloads" == "y" ]; then
  "$outputDir/rune" workload install "vein.runtime@$tagVersion"
  if [ $? -ne 0 ]; then exit 1; fi

  "$outputDir/rune" workload install "vein.compiler@$tagVersion"
  if [ $? -ne 0 ]; then exit 1; fi

  echo "Workloads installed."
else
  echo "Workloads installation skipped."
fi


shell_config_file=""

if [ "$SHELL" == "/bin/zsh" ] || [ "$SHELL" == "/usr/bin/zsh" ]; then
  shell_config_file="$HOME/.zshrc"
elif [ "$SHELL" == "/bin/bash" ] || [ "$SHELL" == "/usr/bin/bash" ]; then
  if [ "$(uname)" == "Darwin" ]; then
    shell_config_file="$HOME/.bash_profile"
  else
    shell_config_file="$HOME/.bashrc"
  fi
else
  echo "Unsupported shell: $SHELL"
  exit 1
fi

if ! echo "$PATH" | grep -q "$outputDir/bin"; then
  echo "Updating PATH..."
  if ! grep -Fxq "export PATH=\$PATH:$outputDir/bin" "$shell_config_file"; then
    echo "export PATH=\$PATH:$outputDir/bin" >> "$shell_config_file"
    echo "PATH updated in $shell_config_file"
  else
    echo "PATH already configured in $shell_config_file"
  fi
  source "$shell_config_file"
else
  echo "PATH already contains $outputDir/bin"
fi

echo "Rune installed. Please restart your terminal to apply changes."