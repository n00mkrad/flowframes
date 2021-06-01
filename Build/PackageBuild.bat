@echo off

echo ============================================
echo ==== NMKD'S FLOWFRAMES PACKAGING SCRIPT ====
echo ============================================
echo.

set /p ver="Enter the version number: "
set /p packSlim="Package Slim (no Pytorch) archive? (Y/N) "
set /p packFull="Package Full (Pytorch for Turing and older) archive? (Y/N) "
set /p packFull3000="Package Full-RTX3000 (Pytorch for Ampere and older) archive? (Y/N) "
set /p packWebBase="Package web installer (base.7z) file? (Y/N) "

cd ..\Code\bin\x64\Release

rmdir /s/q FlowframesApp%ver%
mkdir "FlowframesApp%ver%"
mkdir "FlowframesApp%ver%/FlowframesData"
mkdir "FlowframesApp%ver%/FlowframesData/pkgs"

rem xcopy "../../../../pkgs" "FlowframesApp%ver%/FlowframesData\pkgs\" /E
xcopy "../../../../pkgs/av" "FlowframesApp%ver%/FlowframesData\pkgs\av" /E /I
xcopy "../../../../pkgs/dain-ncnn" "FlowframesApp%ver%/FlowframesData\pkgs\dain-ncnn" /E /I
xcopy "../../../../pkgs/licenses" "FlowframesApp%ver%/FlowframesData\pkgs\licenses" /E /I
xcopy "../../../../pkgs/rife-cuda" "FlowframesApp%ver%/FlowframesData\pkgs\rife-cuda" /E /I
xcopy "../../../../pkgs/rife-ncnn" "FlowframesApp%ver%/FlowframesData\pkgs\rife-ncnn" /E /I
xcopy "../../../../pkgs/flavr-cuda" "FlowframesApp%ver%/FlowframesData\pkgs\flavr-cuda" /E /I

echo %ver% >> "FlowframesApp%ver%/FlowframesData/ver.ini"

xcopy Flowframes.exe "FlowframesApp%ver%"

cd ../../../../Build

rmdir /s/q ..\Code\bin\x64\Release\FlowframesApp%ver%\FlowframesData\logs
del ..\Code\bin\x64\Release\FlowframesApp%ver%\FlowframesData\config.ini


IF /I "%packSlim%"=="Y" (
	rem PACK SLIM
	7za.exe a FF-%ver%-Slim.7z -m0=flzma2 -mx5 "..\Code\bin\x64\Release\FlowframesApp%ver%"
)

echo 1
IF /I "%packWebBase%"=="Y" (
	echo 2
	rem PACK WEB BASE
	rem mkdir "WebInstaller/%ver%/base.7z"
	7za.exe a "WebInstaller/%ver%/base.7z" -m0=flzma2 -mx7 "..\Code\bin\x64\Release\FlowframesApp%ver%\*"
)

IF /I "%packFull%"=="Y" (
	rem PACK FULL
	xcopy "../pkgs/py-tu" "..\Code\bin\x64\Release\FlowframesApp%ver%\FlowframesData\pkgs\py-tu" /E /I
	7za.exe a FF-%ver%-Full.7z -m0=flzma2 -mx7 "..\Code\bin\x64\Release\FlowframesApp%ver%"
	rmdir /s/q ..\Code\bin\x64\Release\FlowframesApp%ver%\FlowframesData\pkgs\py-tu
)

IF /I "%packFull3000%"=="Y" (
	xcopy "../pkgs/py-amp" "..\Code\bin\x64\Release\FlowframesApp%ver%\FlowframesData\pkgs\py-amp" /E /I
	7za.exe a FF-%ver%-Full-RTX3000.7z -m0=flzma2 -mx7 "..\Code\bin\x64\Release\FlowframesApp%ver%"
	rmdir /s/q ..\Code\bin\x64\Release\FlowframesApp%ver%\FlowframesData\pkgs\py-amp
)

rmdir /s/q ..\Code\bin\x64\Release\FlowframesApp%ver%


rem pause