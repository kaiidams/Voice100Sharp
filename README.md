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

### Build voice100_native

```s
```