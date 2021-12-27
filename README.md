# Voice100 for C#

## Build

### Build WORLD for Windows

Build [WORLD](http://www.kisc.meiji.ac.jp/~mmorise/world/english) outside this repository.

```s
git clone https://github.com/mmorise/World.git
md World\build
md World\build
cmake .. -A Win32 -B x86-windows
cmake --build x86-windows --config Release
cmake .. -A x64 -B x86_64-windows
cmake --build x86_64-windows --config Release
```

### Build voice100_native for Windows

```s
md voice100_native\build
cd voice100_native\build
set WORLD_DIR=path\to\World
cmake .. -A Win32 -B win-x86 ^
    -D WORLD_INC=%WORLD_DIR%\src ^
    -D WORLD_LIB=%WORLD_DIR%\build\x86-windows
cmake --build win-x86 --config Release
cmake .. -A x64 -B win-x64 ^
    -D WORLD_INC=%WORLD_DIR%\src ^
    -D WORLD_LIB=%WORLD_DIR%\build\x86_64-windows
cmake --build win-x64 --config Release
```