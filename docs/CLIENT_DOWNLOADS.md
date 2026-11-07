# Windows client download

The home-page card supports an **externally hosted** Windows client download. It is not ready to download until an administrator supplies a permanent public HTTPS link in `config/client_downloads.php`. The supplied `windows['url']` is empty; no download address is invented or included.

## Server administrators

1. Upload `DigimonNXT_Windows_x64_Client_v1.1.0_Dawn_World.zip` once to an external file host. Keep it off the game server.
2. Obtain a permanent public HTTPS download link. Verify that visitors can download the whole ZIP without a hosting account or login. Avoid temporary or expiring links.
3. Open the installed game's `config/client_downloads.php` and paste that link into the `url` value inside `windows`. Keep the remaining release metadata unchanged for this ZIP.
4. Open the home page and test its download link in a private browser window.

The button links directly to the external host. The game application does not fetch, mirror, proxy or cache the client ZIP, and it has no local download fallback. The file host supplies the ZIP download bandwidth. Website pages, multiplayer traffic and requested game assets still use the game server normally.

For an existing installation, copy the update's `public_html/` contents into the game web root, then configure the URL. For a new installation, deploy the full source and configure the same setting. No database migration, schema repair or setup action is required for this feature.

**Remove the previous local download**, if installed. Inside the installed game folder, delete:

`downloads/DigimonNXT_Windows_x64_Client_v1.1.0_Dawn_World.zip`

This is under `public_html/downloads/` in an extracted source package.

Leaving that old file reachable would let old links consume game-server bandwidth. The update includes an Apache deny rule for the legacy download path; on other web servers, remove the old ZIP or explicitly block that path. The external download does not need a ZIP in `public_html/downloads/`.

| Release metadata | Value |
| --- | --- |
| Client version | 1.1.0 |
| Filename | `DigimonNXT_Windows_x64_Client_v1.1.0_Dawn_World.zip` |
| Size | 286,007,739 bytes |
| SHA-256 | `5208e882be770141a9947ad37ab7d04c721168e1dced5fd9849336e8d4880d82` |

For a future release, upload the new external package and update the URL, filename, version, byte size and SHA-256 together. Test the public download before announcing it.

## Players

Once the download is available, extract the **entire ZIP** and open `DigimonNXT.exe`. Keep its accompanying files and `GameAssets` folder together. The .NET runtime is included. If Microsoft Edge WebView2 is missing, use the client's installer button or `Install WebView2.cmd`; the included official installer needs internet access.

Use **Server & options** to enter the game server's domain or IP address and game folder. The default, `http://127.0.0.1/DigimonNXT/`, connects to a server on your own PC, not automatically to the website you downloaded from. Domains, subdomains and custom ports are supported.

Use Windows 11 x64, or a supported servicing edition of Windows 10 x64, with WebView2 Evergreen Runtime. Client 1.1.0 retains 200% default game zoom and F11 fullscreen. It is an unsigned release. Game accounts and progress remain on the selected game server.
