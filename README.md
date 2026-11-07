# Digimon NXT v0.7.2 — Automatic XAMPP setup

**External client download:** The homepage card links directly to an external file host, so the game server does not upload the client ZIP to players. Set `windows['url']` in `public_html/config/client_downloads.php` to the client ZIP's permanent public HTTPS link. No external URL has been supplied yet, so the download is unavailable until that setting is filled in. The small **External_Client_URL_Update ZIP** updates an existing game without database setup. It includes an Apache rule blocking the retired local download directory; remove any old client ZIP from your installed `downloads/` folder as well. The full source already includes this correction and needs no companion client package. See [external download setup](docs/CLIENT_DOWNLOADS.md).

**Battle animations:** Quick attack movement, elemental skill effects, per-hit damage, critical and effectiveness feedback, healing and status cues, and synchronized original Dawn sound. Commands resolve in place so the soundtrack continues. New Battle Park matches involving players support recorded replays with speed and skip controls.

This is the **automatic XAMPP password setup edition**, retaining the v0.7.1 sprite direction fix, full Dawn world and local server console. Create / Repair Schema and Fresh Rebuild now change the actual local MySQL root password and update the installed game configuration automatically. No separate updater or manual config edit is needed. Use repair to keep saves. See [battle controls](docs/BATTLE_PRESENTATION.md).

Explore all **264 supplied Dawn maps**, assembled from 351 uploaded layers, at their supplied resolution. The eleven older generated maps are replaced by the Dawn atlas: 30 environment groups, visible wild Digimon, nearby players and AI tamers, travel gates and 481 accessible floor sections. Collision comes from the original ROM geometry. Three-versus-three combat, full-party restoration, Battle Park, 1,000 persistent rivals and the local world-server console remain included.

## Install or upgrade

1. Start **Apache** and **MySQL** in XAMPP.
2. Copy the contents of `public_html/` into `C:\xampp\htdocs\DigimonNXT\`. For an upgrade, back up your files/database and preserve the installed `config/app.php`; setup updates only its database password and keeps your custom settings. Copy the new `config/version.php` and the other application files.
3. The supplied defaults are database `digimon_nxt`, user `root`, password `QuestionMark123456?`, port `3306`, base path `/DigimonNXT`. Keep your own database name, host, port and game path when upgrading. Local setup automatically handles a blank XAMPP root password, the currently configured password or the requested password.
4. On the server PC, open `http://localhost/DigimonNXT/_setup.php` and choose **Create / Repair Schema**. This release keeps schema 8 and 43 tables. Saved players, collections, parties and rival accounts are preserved.
5. Restart the existing background tamer task after copying updated PHP files. For a fresh installation, run `tools/install_tamer_service_windows.bat` once; it targets the installed game folder and reads that folder's database settings.
6. Open **Dawn World**, choose a destination and start exploring. Previous Cyberspace bookmarks resolve to safe Dawn destinations; positions are checked and repaired as players revisit maps.

**Do not use Fresh Rebuild to upgrade saved accounts:** it deletes the configured game database. Both setup actions synchronize the local MySQL root password to `QuestionMark123456?`. If MySQL has a different password that is no longer saved in the game configuration, enter it once in the masked field on the setup page. See [START_HERE.txt](START_HERE.txt) and [installation instructions](docs/INSTALLATION.md).

## Explore Dawn World

- Search the atlas by area name or number. Filter by environment and wild/rest areas; pages show 24 destinations at a time.
- Travel through a destination card, a connection in the map sidebar, or a marked gate. Use **Area landings** to reach separate paths or platforms within a map.
- Move with WASD, arrows, numpad or the movement pad. The camera follows your tamer while the map retains its natural image dimensions.
- Select a visible Digimon, or press **E** for the nearest signal. Approach along clear ground until it is in range, then choose **Battle**.
- Fight with three active partners and up to three reserves. Scan data is secured when a valid encounter starts. Every completed battle, including defeat or disconnect, restores the entire party's HP and SP.
- Follow genuine rival progress in **Tamer Activity** and **Tamer Rivals**. The persistent Windows task keeps bots active even with every browser closed.

[Dawn World guide](docs/DAWN_WORLD.md) · [All 264 maps](docs/DAWN_WORLD_MAPS.md) · [Gameplay rules](docs/GAMEPLAY.md) · [Ranked play and bots](docs/RANKED_AND_BOTS.md)

## Local administrator console

Run `tools/start_world_server_windows.bat` on the physical server PC and accept administrator elevation. The existing **52 commands** cover accounts, passwords, bans, kicks, money, Digimon, items, announcements, maintenance and bots. Live events and the editable prompt share one window; repeated traffic is grouped. Start with `help` and `status`.

The console uses Windows PowerShell 5.1 and the installed game's configuration. It has no browser command panel or network command listener. Closing it leaves Apache, MySQL, the background tamer task and other MMOs running. See [the console guide](docs/WORLD_SERVER_CONSOLE.md) and [complete command reference](WORLD_SERVER_COMMANDS.txt).

## Windows client

The separate **Windows x64 client v1.1.0** includes the new maps, thumbnails, sprites and music. It uses Microsoft Edge WebView2, retains the existing server/preferences workflow and checks bundled assets against the selected server's read-only manifest. Matching files are served locally; changed files are fetched as needed. The server's `_client_manifest.php` is included in this release; no separate server add-on is needed.

Extract the entire new client ZIP into a new folder and run `DigimonNXT.exe`. Existing local preferences and login profiles are retained. Its default server is `http://127.0.0.1/DigimonNXT/`; server settings accept IP addresses, domains, subdomains, ports and custom game paths. The client retains its 200% default game zoom and Windows DPI support. No Android rebuild is included in this map update.

## Scope and source accuracy

The supplied map images are exactly twice the original DS graphics dimensions and are displayed at their supplied pixel dimensions in the web page. Movement uses conservative original collision geometry, with every floor section reachable through NXT travel controls. Environment labels describe the artwork; numbered map IDs preserve source identity. NXT gates and landings are not a port of Dawn's story events, puzzles or original warp scripts.

Battle Park remains an NXT adaptation inspired by ReArise, using the requested three-versus-three format. It is asynchronous play against persistent party snapshots. Verified standard species use Dawn sprites; unmatched species and Paradox forms retain their existing artwork. The ROM is not required at runtime.

See [asset provenance](docs/DAWN_ASSETS.md), [collision conversion](docs/DAWN_COLLISION_NOTES.md) and [TESTING.md](TESTING.md) for coverage and actual validation. Runtime requirements are PHP 8.1+ with mysqli/mysqlnd, MySQL 8 or MariaDB 10.4+, and a modern browser.
