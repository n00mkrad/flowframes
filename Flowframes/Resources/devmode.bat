@echo off
if not "%1"=="am_admin" (powershell start -verb runas '%0' am_admin & exit /b)

reg add "HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\AppModelUnlock" /t REG_DWORD /f /v "AllowDevelopmentWithoutDevLicense" /d "1"