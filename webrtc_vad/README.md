# wavrtc_vad

[WebRTC](https://webrtc.org/) VAD
copied from https://webrtc.googlesource.com/src/.

## Build for Windows

```
set CMAKE="c:\Program Files\Microsoft Visual Studio\2022\Community\Common7\IDE\CommonExtensions\Microsoft\CMake\CMake\bin\cmake.exe"

set ARCH=x64
set DIST=win-x64

%CMAKE% ^
    -A x64 ^
    -B build\%DIST% ^
    .
cd build\%DIST%
msbuild webrtc_vad.sln -p:Configuration=Release
md ..\..\dist\%DIST%
copy Release\webrtc_vad.dll ..\..\dist\%DIST%
```