@echo off

echo ===============================
echo == NMKD'S FLOWFRAMES BUILDER ==
echo ===============================
echo.
echo This script makes a build ready for distribution by creating three 7z archives, without python, with pytorch for Turing, and with pytorch for Ampere.
echo.

set "ver=16"
set /p ver="Enter the version number: "

cd ..

dotnet publish -c Release -o "Release/Build" "Code5"

cd Release

rmdir /s/q FlowframesApp%ver%
mkdir "FlowframesApp%ver%"
mkdir "FlowframesApp%ver%/FlowframesData"
mkdir "FlowframesApp%ver%/FlowframesData/pkgs"

rem xcopy "../pkgs" "FlowframesApp%ver%/FlowframesData\pkgs\" /E
xcopy "../pkgs/av" "FlowframesApp%ver%/FlowframesData\pkgs\av" /E /I
xcopy "../pkgs/dain-ncnn" "FlowframesApp%ver%/FlowframesData\pkgs\dain-ncnn" /E /I
xcopy "../pkgs/licenses" "FlowframesApp%ver%/FlowframesData\pkgs\licenses" /E /I
xcopy "../pkgs/rife-cuda" "FlowframesApp%ver%/FlowframesData\pkgs\rife-cuda" /E /I
xcopy "../pkgs/rife-ncnn" "FlowframesApp%ver%/FlowframesData\pkgs\rife-ncnn" /E /I

echo %ver% >> "FlowframesApp%ver%/FlowframesData/ver.ini"

xcopy /E "Build" "FlowframesApp%ver%"

cd ..\Build5

rmdir /s/q ..\Release\FlowframesApp%ver%\FlowframesData\logs
del ..\Release\FlowframesApp%ver%\FlowframesData\config.ini


7za.exe a FF-%ver%-Slim.7z -m0=flzma2 -mx5 "..\Release\FlowframesApp%ver%"

xcopy "../pkgs/py-tu" "..\Release\FlowframesApp%ver%\FlowframesData\pkgs\py-tu" /E /I
7za.exe a FF-%ver%-Full.7z -m0=flzma2 -mx7 "..\Release\FlowframesApp%ver%"

rmdir /s/q ..\Release\FlowframesApp%ver%\FlowframesData\pkgs\py-tu
xcopy "../pkgs/py-amp" "..\Release\FlowframesApp%ver%\FlowframesData\pkgs\py-amp" /E /I
7za.exe a FF-%ver%-Full-RTX3000.7z -m0=flzma2 -mx7 "..\Release\FlowframesApp%ver%"


rmdir /s/q ..\Release\FlowframesApp%ver%


rem pause