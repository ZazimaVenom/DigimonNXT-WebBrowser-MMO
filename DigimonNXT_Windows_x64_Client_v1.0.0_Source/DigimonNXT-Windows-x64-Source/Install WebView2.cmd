@echo off
setlocal
cd /d "%~dp0"
if exist "Runtime\MicrosoftEdgeWebview2Setup.exe" (
  start "" /wait "Runtime\MicrosoftEdgeWebview2Setup.exe" /install
) else (
  start "" "https://developer.microsoft.com/microsoft-edge/webview2/"
)
echo.
echo After Microsoft Edge WebView2 finishes installing, open DigimonNXT.exe.
pause
