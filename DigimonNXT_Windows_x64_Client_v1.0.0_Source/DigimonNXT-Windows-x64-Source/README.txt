DIGIMON NXT — WINDOWS x64 CLIENT 1.0.0
For the v0.5.1 Always Active Tamers server baseline

PLAY
1. Extract the ENTIRE client ZIP to a folder. Do not run inside the ZIP.
2. Open DigimonNXT.exe. .NET is included; no .NET installation is needed.
3. If Microsoft Edge WebView2 is missing, use the installer button or run
   Install WebView2.cmd. The official bootstrapper needs internet access.
4. The client tries http://127.0.0.1/DigimonNXT/ by default.
   Start your existing XAMPP Apache and MySQL services first.
5. Choose Server & options at any time to enter another game server.

SERVER ADDRESSES
- This PC: http://127.0.0.1/DigimonNXT/
- Another PC on your network: http://192.168.1.20/DigimonNXT/
- Domain / subdomain: https://play.example.com/DigimonNXT/
- Custom port: http://192.168.1.20:8080/DigimonNXT/
- Game at domain root: https://play.example.com/
An address without a path uses /DigimonNXT/. An explicit / means domain root.
A full URL keeps its explicit game folder.
Enter only the base game address, not a URL with query parameters.
Each server gets a separate browser profile so sessions remain isolated.

DISPLAY AND CONTROLS
- Default game zoom: 200%. Change from 100% to 250% in Server & options.
- Native Windows interface is per-monitor DPI aware for crisp 4K text.
- Original pixel sprites retain their artwork; zoom does not invent detail.
- F11: full screen. Escape exits full screen. F5: reload. Alt+Left: back.
- Login, party, combat, audio and the existing game interface run through
  the real Microsoft Edge WebView2 engine in a native WPF Windows client.
- Progress and bots stay on your game server. Closing the client does not
  stop an installed Always Active Tamers background worker.

BUILT-IN ASSETS
GameAssets includes the baseline sprites, UI, maps and audio. Files are read
only when the game requests them. Nothing preloads the whole collection.
Install the optional native client bridge on your server for version-checked
local assets. Matching files are SHA-256 verified and served locally. Changed
files download only on demand, are verified against the current server
manifest and enter a bounded 512 MB per-server disk cache. The manifest is
checked on connection and every minute using ETag / HTTP 304 to avoid
re-downloading an unchanged manifest; invalid/unavailable manifests fall
back to ordinary server requests. Existing servers remain playable without
the bridge, using the WebView2 HTTP cache normally. No PHP, passwords or
private game responses are stored in the public asset cache.

DATA AND RUNTIME
Preferences, browser profiles and cache live in:
%LOCALAPPDATA%\DigimonNXT\Client
Use Windows 10 x64 (supported servicing edition) or Windows 11 x64 with a
current Microsoft Edge WebView2 Evergreen Runtime. Windows 11 is preferred.
The EXE is an unsigned development release: Windows may show SmartScreen.
No administrator rights are needed for normal gameplay.
HTTPS certificate errors are not bypassed. Use HTTP only for trusted local
servers; use HTTPS for internet servers. No JavaScript/native command bridge
is exposed to the game. File/data navigations and in-client downloads are
blocked. External HTTP(S) links ask before opening in the default browser.

SOURCE BUILD
Install .NET 10 SDK, then run build-windows.ps1 from a fully extracted source
ZIP. It restores the pinned WebView2 SDK and emits a self-contained x64 build
into dist/Windows-x64. Internet is needed for the first dependency restore.
Microsoft WebView2 SDK: 1.0.4191.47. Client version: 1.0.0.
The separate runtime can update automatically without rebuilding this app.
The source was cross-compiled on Linux. Real Windows display/installer smoke
checks are still required on the target machine; this package does not claim
that a native Windows execution test ran in the build environment.

REFRESH PACKAGED GAME ASSETS (source ZIP)
Install Python 3, then run this from the Windows source project folder:

python tools/build_asset_bundle.py --web-root "C:\xampp\htdocs\DigimonNXT" --output . --game-version 0.5.1

Use the actual server web folder containing assets/, html/static/ and
favicon.png. For an extracted server source ZIP, select its public_html folder.
Change --game-version when updating the server baseline. The builder copies
allowed public files into GameAssets, updates both manifests and removes
retired bundle files, so stale sprites/scripts are not carried into a release.
It excludes PHP, private configuration, databases and symlinks. Python's
standard library is sufficient; no pip packages are required.
Run build-windows.ps1 afterward to rebuild dist/Windows-x64 with the new bundle.
