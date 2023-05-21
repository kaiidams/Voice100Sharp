$AndroidNDK = "C:\Microsoft\AndroidNDK"
$AndroidNDKVersion= "android-ndk-r23c"
$AndroidCMakeToolchainFile = "$AndroidNDK\$AndroidNDKVersion\build\cmake\android.toolchain.cmake"
$WorldDir = $(Get-Location).Path + "\third_parties\World"

$AllNames = @(
    "android-arm64-v8a-release",
    "android-armeabi-v7a-release",
    "android-x86-release",
    "android-x86_64-release",
    "win-x86",
    "win-x64"
)

function Run-Build
{
    param (
        $Name,
        $Project
    )

    $Args = @(".", "-B", ".\out\build\$Name")
    if ($Name -match "^android-(.+)-(release|debug)$")
    {
        $AndroidABI = $Matches.1
        $Args += @("-D", "CMAKE_TOOLCHAIN_FILE=$AndroidCMakeToolchainFile")
        $Args += @("-D", "ANDROID_ABI=$AndroidABI", "-D", "ANDROID_NATIVE_API_LEVEL=26")
        $Args += @("-G", "Ninja")
        if ($Project -ne "World")
        {
            $Args += @(
                "-D",
                "WORLD_INC=$WorldDir\out\install\win-x64-release\include",
                "-D",
                "WORLD_LIB=$WorldDir\out\install\$Name\lib")
        }

        cmake $Args
        if ($Project -eq "World")
        {
            cmake --build .\out\build\$Name --config Release --target lib/libworld.a
        }
        else
        {
            cmake --build .\out\build\$Name --config Release
        }
        cmake --install .\out\build\$Name --prefix .\out\install\$Name
    }
    else
    {
        if ($Name.StartsWith("win-x64"))
        {
            $Args += @("-A", "x64")
        }
        else
        {
            $Args += @("-A", "Win32")
        }
        if ($Project -ne "World")
        {
            $Args += @(
                "-D",
                "WORLD_INC=$WorldDir\out\install\$Name\include",
                "-D",
                "WORLD_LIB=$WorldDir\out\install\$Name\lib")
        }
        cmake $Args
        cmake --build .\out\build\$Name --config Release
        cmake --install .\out\build\$Name --prefix .\out\install\$Name
    }

}

function Make-AndroidAAR
{
    Remove-Item ".\out\temp.zip"
    Remove-Item -Recurse ".\out\temp"
    New-Item -Path ".\out\temp" -Type directory
    foreach ($Name in $AllNames)
    {
        if ($Name -match "^android-(.+)-(release|debug)$")
        {
            $AndroidABI = $Matches.1
            $dest = ".\out\temp\jni\$AndroidABI"
            New-Item -Path $dest -Type directory
            Copy-Item ".\out\install\$Name\lib\*.so" $dest
        }
    }
    Copy-Item AndroidManifest.xml ".\out\android-release"
    Compress-Archive ".\out\temp\*" .\out\temp.zip
    New-Item -Path ".\out\install\android-release" -Type directory
    Move-Item .\out\temp.zip ".\out\install\android-release\voice100_native.aar"
    Remove-Item -Recurse ".\out\temp"
}

pushd third_parties\World
Run-Build -Name win-x64-release -Project World
Run-Build -Name win-x86-release -Project World
Run-Build -Name android-arm64-v8a-release -Project World
Run-Build -Name android-armeabi-v7a-release -Project World
Run-Build -Name android-x86-release -Project World
Run-Build -Name android-x86_64-release -Project World
popd
pushd Voice100.Native
Run-Build -Name win-x64-release -Project native
Run-Build -Name win-x86-release -Project native
Run-Build -Name android-arm64-v8a-release -Project native
Run-Build -Name android-armeabi-v7a-release -Project native
Run-Build -Name android-x86-release -Project native
Run-Build -Name android-x86_64-release -Project native
Make-AndroidAAR
popd