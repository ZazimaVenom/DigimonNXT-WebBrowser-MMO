# Digimon NXT clients 1.0.0

For the Digimon NXT 0.5.1 Always Active Tamers server baseline.

## Start playing

1. Keep your existing game and database. Copy `_client_manifest.php` from the server asset bridge ZIP into your game's web folder, beside `index.php` and `_health.php`. For XAMPP this is normally `C:\xampp\htdocs\DigimonNXT\`. This enables verified local assets without any database changes.
2. Windows: extract the entire Windows x64 client ZIP, then open `DigimonNXT.exe`. If requested, use the included official Microsoft Edge WebView2 runtime installer. The .NET runtime is included.
3. Android: install the release APK on Android 8.0 or later. Allow APK installation from the app you use to open it when prompted.
4. The initial target on both platforms is `http://127.0.0.1/DigimonNXT/`. The native server screen is available immediately. On Android, enter your game PC's LAN address, because `127.0.0.1` refers to the phone itself. A phone and PC on the same Wi-Fi can use a URL such as `http://192.168.1.20/DigimonNXT/`, provided Apache accepts LAN connections.
5. A bare public domain or subdomain selects HTTPS; a bare local/IP address selects HTTP. Addresses without a path use `/DigimonNXT/`. An explicit `/` means a game installed at the domain root. Full URLs support custom ports and folders.

## Included behavior

- Windows has a native WPF interface and Microsoft Edge WebView2; Android has a native Android interface and Android System WebView. The server game runs in the embedded browser. These are dedicated clients, not an offline reimplementation of the game engine.
- Windows is per-monitor DPI aware and starts at 200% game zoom. Android follows native display density and starts at 200% game text size, with pinch zoom. Both have readability controls. Original pixel artwork is preserved rather than artificially adding detail.
- Both clients include 3,476 baseline public assets, including sprites, artwork, maps and music. Matching assets are opened locally only when requested. The optional server bridge identifies changed files; replacements are downloaded on demand, SHA-256 verified and kept in a bounded cache. A small manifest check uses ETag/304 responses when unchanged.
- Accounts, battles, ranked results, bots and saves stay on the game server. The existing always-active bot worker must remain running on that server; the client does not replace it.
- Server settings are remembered. Windows isolates browser profiles by full server address. Android clears the prior embedded browser session when changing servers. Same-server reloads retain login.

## Source and updates

The Windows and Android source ZIPs are separate and each includes its complete asset bundle, reproducible build instructions, validation notes and the asset regeneration tool. Development toolchains are not included. First-time builds need internet access to restore the documented dependencies.

The separate Android private signing backup contains the key and credentials for the supplied APK. Keep it private and backed up: later APKs need the same key and a higher versionCode to update an existing installation. The private key is deliberately absent from both source ZIPs.

## Verification limits

Build, package, URL/origin and asset transport checks are documented in each source package's TESTING file. The Windows executable was cross-compiled on Linux; its actual Windows interface and runtime installation have not been executed here. The Windows executable is unsigned and may show a Windows reputation prompt. Physical Android device behavior, speakers/audio, keyboards and device-specific WebView differences still require testing on the intended hardware. See the Android TESTING file for the result of the emulator smoke check.
