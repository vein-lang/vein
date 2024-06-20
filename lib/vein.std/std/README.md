---

> Vein is an open source high-level strictly-typed programming language with a standalone OS, arm and quantum computing support.

---

## OS Support

OS                            | Version                       | Architectures
------------------------------|-------------------------------|----------------
Windows 10                    | 1607+                         | x64, ARM64
OSX                           | 10.14+                        | x64
Linux                         |                               | x64, ARM64


## Compiling from source

### Building on Windows

For building, you need the following tools:
- dotnet 6.0
- Win10 SDK
- vsbuild-tools-2019 with MSVC 2019, MSVC142 for ARM64


Checkout mana sources
```bash
git clone https://github.com/vein-lang/vein.git --recurse-submodules
cd ./vein-lang
git fetch --prune --unshallow --tags

dotnet restore
```

#### Compile IshtarVM
Go to ishtar folder
```base
cd ./runtime/ishtar.vm
```
Compile for Windows 10 x64
```bash
dotnet publish -r win10-x64 -c Release
```
Compile for Windows 10 ARM64
```
dotnet publish -r win-arm64 -c Release
```