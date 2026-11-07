# Digimon NXT Windows client 1.0.0 — verification

Built with Microsoft .NET SDK 10.0.401, target `net10.0-windows`, runtime
`win-x64`, and pinned Microsoft WebView2 SDK 1.0.4191.47.

The native WPF project cross-compiled and self-contained publish completed
without warnings or errors on Linux. The generated executable is a Windows
x86-64 PE GUI app; the .NET runtime, Windows Desktop assemblies and native
WebView2 x64 loader are included. The official Microsoft Edge WebView2
Evergreen bootstrapper is bundled separately.

The actual production `ServerAddress`, `AssetCache`, and settings classes
were compiled into portable .NET 10 test harnesses and executed on Linux:

- 31 address/origin checks passed: IP, domain, custom ports, IPv6, explicit
  game folders/root hosting, HTTPS, default paths, cross-origin boundaries,
  and invalid traversal/encoded separator/credential inputs.
- 14 asset-cache checks passed against a real loopback HTTP fixture:
  matching bundle causes no asset download; changed hashes download once;
  cached updates reuse bytes; mismatched hashes fall through; failed
  manifests disable stale overrides; deleted assets and PHP pages are not
  intercepted; cache data is isolated by server scheme, port and folder. ETag refreshes
  send If-None-Match, and HTTP 304 keeps the last verified manifest.

Run these source tests with the .NET 10 SDK:

```
dotnet run --project Tests/Url/UrlChecks.csproj -c Release
dotnet run --project Tests/Cache/CacheChecks.csproj -c Release
```

The native WPF window, Microsoft runtime installation, real Windows DPI
transitions, controller/keyboard focus, actual WebView2 sound seeking, and
on-device gameplay could not be executed on this Linux host. Complete a
Windows smoke test using the steps below before broad redistribution.

1. Extract the complete portable build on Windows x64.
2. Install WebView2 if prompted and reopen DigimonNXT.exe.
3. Start XAMPP and confirm automatic localhost connection.
4. Stop Apache: connection errors must keep server selection available.
   Edit the address during a connection attempt: the previous attempt must
   cancel, and the new address must remain ready to connect.
5. Connect to an explicit LAN server and a HTTPS subdomain/game folder.
6. Log in; load the world map, change areas and party, battle, and play audio.
7. Confirm the footer reports matching built-in assets with the optional
   server bridge installed. Without the bridge, gameplay uses server assets.
8. In Server & options select 100%, 150%, 200%, 250%; restart to verify
   persistence. Test 4K and mixed-DPI monitors, F11 and Alt+Left.
9. Switch between two game servers; sessions must remain separate.
10. Close the client and verify the existing server bot service stays active.
