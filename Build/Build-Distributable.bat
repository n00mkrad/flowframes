rem @echo off

echo ===============================
echo == NMKD'S FLOWFRAMES BUILDER ==
echo ===============================
echo.
echo This script makes a build ready for distribution by creating two 7z archives, one with and one without embedded python.
echo.

set "ver=16"
set /p ver="Enter the version number: "

cd ..\Code\bin\x64\Release

rmdir /s/q FlowframesApp%ver%
mkdir "FlowframesApp%ver%"
mkdir "FlowframesApp%ver%/FlowframesData"
mkdir "FlowframesApp%ver%/FlowframesData/pkgs"

xcopy "../../../../pkgs" "FlowframesApp%ver%/FlowframesData\pkgs\" /E

echo %ver% >> "FlowframesApp%ver%/FlowframesData/ver.ini"

xcopy Flowframes.exe "FlowframesApp%ver%"

cd ../../../../Build

7za.exe a FlowframesApp%ver%-Full.7z -m0=flzma2 -mx7 "..\Code\bin\x64\Release\FlowframesApp%ver%"
rmdir /s/q ..\Code\bin\x64\Release\FlowframesApp%ver%\FlowframesData\pkgs\py
rmdir /s/q ..\Code\bin\x64\Release\FlowframesApp%ver%\FlowframesData\pkgs\py-tu
rmdir /s/q ..\Code\bin\x64\Release\FlowframesApp%ver%\FlowframesData\pkgs\py-amp
7za.exe a FlowframesApp%ver%-NoPython.7z -m0=flzma2 -mx5 "..\Code\bin\x64\Release\FlowframesApp%ver%"

rmdir /s/q ..\Code\bin\x64\Release\FlowframesApp%ver%


pause