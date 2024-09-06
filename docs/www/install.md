# Installation


::: tip
Linux users — The unzip, jq and curl package is required to install VeinSDK. 
Use sudo apt install unzip jq curl to install packages. 
Kernel version 5.6 or higher is strongly recommended, but the minimum is 5.1. Use uname -r to check Kernel version.
:::


::: code-group

```powershell [windows]
irm "https://vein-lang.org/install.ps1" | iex
```

```bash [macOS/Linux]
curl -fsSL https://vein-lang.org/install.sh | bash 
```
:::

:::tip
VeinSDK requires a minimum of Windows 10 version 1809
:::

## Checking installation

```bash [macOS/Linux]
$ rune --version
Vein Rune CLI 0.*.*
Branch: master+*
```

## Checking workloads installed

```bash [macOS/Linux]
$ rune workload list
```


## Installing VSCode Extensions


```bash [console]
code --install-extension veinlang.devkit
```

or download it manualy [Vein DevKit](https://marketplace.visualstudio.com/items?itemName=veinlang.devkit)


## Uninstalling


::: code-group

```powershell [windows]
rm ~\.vein
```

```bash [macOS/Linux]
rm -rf ~/.vein
```


:::
