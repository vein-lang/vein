<!-- warning -->
<p align="center">
  <a href="#">
    <img src="https://user-images.githubusercontent.com/13326808/118900923-c3c9c280-b91a-11eb-94f3-691feb53e0f4.png">
  </a>
</p>

<p align="center">
  <a href="#">
    <img height="256" src="https://raw.githubusercontent.com/vein-lang/artwork/main/vein-poster.png">
  </a>
</p>
<!-- classic badges -->
<p align="center">
  <a href="https://www.codacy.com/gh/vein-lang/vein/dashboard?utm_source=github.com&amp;utm_medium=referral&amp;utm_content=vein-lang/vein&amp;utm_campaign=Badge_Grade">
    <img src="https://app.codacy.com/project/badge/Grade/ef714af013574e85acfa3c8d59434c47">
  </a>
  <a href="https://www.codacy.com/gh/vein-lang/vein/dashboard?utm_source=github.com&utm_medium=referral&utm_content=vein-lang/vein&utm_campaign=Badge_Coverage">
    <img src="https://app.codacy.com/project/badge/Coverage/ef714af013574e85acfa3c8d59434c47">
  </a>
  <a href="#">
    <img src="https://img.shields.io/:license-MIT-blue.svg">
  </a>
  <a href="https://github.com/vein-lang/vein/releases">
    <img src="https://img.shields.io/github/release/vein-lang/vein.svg?logo=github&style=flat">
  </a>
</p>
<!-- popup badges -->
<p align="center">
  <a href="https://t.me/ivysola">
    <img src="https://img.shields.io/badge/Ask%20Me-Anything-1f425f.svg?style=popout-square&logo=telegram">
  </a>
</p>

<!-- big badges -->
<p align="center">
  <a href="#">
    <img src="https://forthebadge.com/images/badges/0-percent-optimized.svg">
    <img src="https://forthebadge.com/images/badges/ctrl-c-ctrl-v.svg">
    <img src="https://forthebadge.com/images/badges/kinda-sfw.svg">
    <img src="https://forthebadge.com/images/badges/powered-by-black-magic.svg">
  </a>
</p>
<p align="center">
  <a href="#">
    <img src="https://forthebadge.com/images/badges/works-on-my-machine.svg">
  </a>
</p>

<!-- Logo -->
<p align="center">
  <a href="#">
    <img src="https://user-images.githubusercontent.com/13326808/118315186-dba9dc80-b4fd-11eb-8d2f-32e8313ba6a7.png">
  </a>
</p>



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

Copy output files
```bash
mkdir output
cp -R ./runtime/ishtar.vm/bin/net6.0/win10-x64/native/ ./output
```

The `output` folder should contain:
- ishtar.exe - main ishtar file
- ishtar.exp - export metadata for main module
- ishtar.lib - dynamic library for main module
- ishtar.pdb - debug symbols


#### Compile veinc
Go to vein compiler folder
```base
cd .\compiler
```
Compile
```
dotnet publish -r win-x64 -c Release
```
Copy the output files
```bash
mkdir output
cp -R ./bin/Release/net6.0/win-x64/publish ./output
```

The `output` folder should contain:
- veinc.exe - main executable compiler file


### Building on Linux (on ubuntu)

For building, you need the following tools:
- dotnet 6.0
- clang
- zlib1g-dev
- libkrb5-dev
- libssl-dev  
    
and additional for arm64 (or maybe using [prebuiled docker image](https://github.com/vein-lang/docker-arm64))
- clang-9
- binutils-arm-linux-gnueabi
- binutils-aarch64-linux-gnu
- crossbuild-essential-arm64
- gcc-multilib
- qemu 
- qemu-user-static 
- binfmt-support 
- debootstrap

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
##### Compile for Linux x64
```bash
dotnet publish -r linux-x64 -c Release
```
Compile for Linux ARM64
```
cd ./cross
sudo ./build-rootfs.sh arm64
cd ..
cd ./runtime/ishtar.vm
dotnet publish -r linux-arm64 -c Release -p:CppCompilerAndLinker=clang-9 -p:SysRoot=/home/.tools/rootfs/arm64
```

Copy output files
```bash
mkdir output
cp -R ./runtime/ishtar.vm/bin/Release/net6.0/linux-x64/native ./output
```

#### Compile veinc
Go to vein compiler folder
```base
cd ./compiler
```
Compile
```
dotnet publish -r linux-x64 -c Release
```
Copy output files
```bash
mkdir output
cp -R ./bin/Release/net6.0/linux-x64/publish ./output
```

The `output` folder should contain:
- veinc - main executable compiler file

## Contributing

We welcome everyone to contribute to vein language.
To do so, you need to know a couple of things about the folder structure::

```yaml
/runtime: folder contains all backend vm\generator for vein
  /common: shared code
  /ishtar.base: base abstraction for generator\vm
  /ishtar.generator: logic of IL generator for IshtarVM
  /ishtar.vm: implementation of ishtar vm in C#
/compiler: folder contains source for vein compiler
/lib: folder with common libraries
  /ast: mana AST library, for parsing
  /projectsystem: project system models, for compiler
/lsp: language server for vein lang
/samples: Wow! its samples!
/test: folder with various tests
```

You can run all tests from the root directory with `dotnet test`.

To recompile the vm and the compiler: `dotnet build`.

To recompile the standard library: `veinc ./vein.std/corlib.vproj`.

After your changes are done, please remember to run `dotnet format` to guarantee all files are properly formatted and
then run the full suite with `dotnet test`.

## Q\A

```yaml
Q:
  Why it based on C#?
A:
  Initially, i started developing a virtual machine in C++,
  but there were a lot of difficulties with basic things (such as collections, text formatting, etc.)
  And at some point i saw that microsoft began to develop a fully AOT compiler for dotnet.
  That means we could write in pure C# without using runtime and std, 
  which allows everyone to write such hard things like an OS!
  So I decided - that's it! I'm Definitely writing a virtual machine in C#!

  So, now I'm developing using the C# runtime and the std, but 
  in version 2.0 I'm planning to completely move away from runtime dependencies.

Q:
  This language really support quantum computing?
A:
  Not now, but in future I'm planning to add support for Microsoft Quantum Simulator, 
  next - support for Azure Qunatum or IBM quantum cloud.
  And after the release of stationary quantum extension card (like PCEx128 😃), 
  I'll add support for them too.


```
## Special Thanks

<p align="center">
  <a href="https://www.jetbrains.com/?from=vein_lang">
    <img height="128" wight="128" src="https://raw.githubusercontent.com/vein-lang/vein/master/.github/images/jetbrains-variant-3.png">
  </a>
</p>


## License

Vein Lang is primarily distributed under the terms of both the MIT license and the Apache License (Version 2.0),
with portions covered by various BSD-like licenses.

Check LICENSE files for more information.

## Support
<p align="center">
   <a href="https://ko-fi.com/P5P7YFY5">
    <img src="https://www.ko-fi.com/img/githubbutton_sm.svg">
  </a>
</p>


