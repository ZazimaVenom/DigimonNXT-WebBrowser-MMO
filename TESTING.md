# v0.7.2 External client URL correction

29 focused PHP/Chromium checks passed: unconfigured desktop/mobile layouts, valid external HTTPS links, native browser download directly from a mocked external host, exactly one archive request, no archive prefetch, no game-server ZIP/proxy request, unsafe URL rejection and keyboard focus. All three PHP files pass syntax checks. Normal game/audio asset requests remain separate from the client download. Evidence: `docs/verification/external-download-card.json`.

The production URL is deliberately empty because no real public host/link was supplied. External-host availability and the Apache legacy-directory denial were not runtime-tested. The delivered source contains no client ZIP; administrators should remove any ZIP left from the previous update and supply their actual external URL. Database setup, game systems and the native client binary are unchanged.

---

# v0.7.2 Homepage client download — prior local-hosting edition

Focused checks passed with PHP 8.3 and headless Chromium 141: homepage rendering, visible download/version information, keyboard focus and expandable instructions, no-JavaScript operation, no automatic ZIP fetching, missing-package handling and a custom game base path. Desktop 1440px and mobile 390px/320px layouts fit without horizontal overflow; 720 CSS pixels also checks the available layout width of a 1440px desktop at 200% zoom. Desktop and mobile screenshots were visually reviewed.

The complete hosted ZIP is byte-identical to the verified Windows v1.1.0 package (SHA-256 checked). HTTP HEAD verifies its length and archive type; HTTP GET verifies ZIP bytes; selecting the button starts a named browser download without login. The test cancels the large HTTP download after it starts. All three touched/new PHP files pass syntax checks. See `docs/verification/client-download-card.json`.

Only homepage code, its new stylesheet/metadata/package and release documentation change. Existing database configuration, automatic password setup, battle systems, assets and world-console launchers remain byte-identical to the latest baseline. No database service was needed for these public-page checks. Windows client execution and Apache/XAMPP execution were not repeated for this homepage update.

Earlier release records below are historical evidence for their respective builds.

---

# v0.7.2 Automatic XAMPP setup correction

The current installer changes the actual local MySQL root password when Create / Repair Schema or Fresh Rebuild is submitted. This is distinct from the prior configuration-only package, whose tests started with the server password already configured.

**44 real HTTP/MariaDB checks passed**, beginning with a newly initialized MariaDB 10.11 server whose root password was genuinely blank. The requested-password connection failed before setup. The actual CSRF-protected setup POST then changed root authentication, updated the installed config and completed schema creation. Blank-password authentication subsequently failed.

Both buttons were exercised from blank-root authentication. Also verified configured old passwords, a stale config already containing the target, a masked current-password submission, wrong-password rejection without changes, full root authentication metadata stability on repeat repair, player/save preservation, and a separate MMO database sentinel surviving repair and Fresh Rebuild. Fresh setup/rebuild produced schema 8, 43 tables, 1,000 species and 1,000 bots. HTTP readiness and game login passed. Submitted secrets did not appear in captured responses, URLs or runtime logs.

**5 final focused Host checks passed** after adding loopback Host validation: localhost, 127.0.0.1 and bracketed IPv6 loopback with a port work; valid-CSRF repair succeeds; foreign and lookalike Host requests are refused without changing account metadata. Remote-client rejection, GET non-mutation and invalid-CSRF rejection are covered by the main suite.

**35 pure PHP checks passed** for configuration updates: exact preservation of custom settings/comments, LF/CRLF and array() syntax, insertion of a missing password, repeat-update identity, quoting, and rejection of dynamic/duplicate/escaped keys or return expressions that could shadow the modified password. Local host/user/port/input boundaries fail before any filesystem or database operation. Run `php tools/test_setup_password_config.php` to repeat them. Both changed PHP runtime files pass syntax checks.

Evidence is in `docs/verification/automatic-xampp-setup.json` and `automatic-xampp-host-guard.json`, including tested source hashes. Password values are excluded from the evidence. Source comparison confirms that only `_setup.php` and its new helper change production behavior; battle code, maps, audio, SQL files and world-console launchers are retained.

Platform scope: these are real PHP 8.3 and MariaDB 10.11 tests on Linux, including HTTP setup and TCP account authentication. Windows/XAMPP GUI execution and MySQL 8 salted-password accounts were not run in this environment. The MySQL branch follows the documented SET PASSWORD interface; the native-password flow used by XAMPP was tested against MariaDB. No production database or prior baseline was modified by QA.

Earlier release records below are historical evidence for their respective builds.

---

# v0.7.2 Shared XAMPP password edition validation — prior configuration-only release

All **28 focused database-password checks passed** on isolated MariaDB 10.11 with PHP 8.3. The server was configured with the requested password; blank-password authentication was rejected.

Verified real HTTP fresh Create / Repair installation, current schema 8 with 43 tables, 1,000 species and 1,000 tamers, login and health. Repeat repair preserved the sentinel player's account, wallet, party, scans and inventory. The packaged authentic v5-to-current upgrade and fresh SQL import suites passed independently of repeat repair.

The local console backend connected through its selected game folder. The tamer worker connected and committed a real batch. Missing-password configuration used the updated runtime/setup defaults; an explicitly configured blank password remained an override and correctly failed authentication against the password-protected server.

Fresh Rebuild ran only against the disposable game database and recreated the complete clean schema and seeds. A separate database representing another MMO retained its sentinel data through repair and rebuild. The shipped source does not alter MySQL users or passwords. Production databases and the previous baseline were not modified. Windows console GUI execution was not part of this connection-only check.

Only the three database-password literals in `config/app.php`, `includes/bootstrap.php` and `_setup.php` change production behavior. All other runtime files, game assets, SQL files and console launchers are identical to the delivered battle-animation baseline. PHP syntax checks passed for all three modified files. The current release evidence is `docs/verification/shared-password-v072.json`.

The earlier battle-animation and historical release validation below describes the builds actually tested at the time, including their original blank-password configuration.

---

# v0.7.2 Battle animation validation — original blank-password build

This is the blank-password release. The current v0.7.1 game code, corrected tamer facing, all Dawn maps/assets and local console are preserved. Database schema remains 8; no migration is needed from v0.7.1. Validation ran on Linux with PHP 8.3, MariaDB 10.11 and headless Chromium.

- **1,580 combat-presentation checks passed**, covering actual hit values, metadata and final-state reconciliation across all 768 moves, plus bounded transient transport. An independent deterministic comparison with the v0.7.1 simulator passed **3,084 scenarios**, with identical outcomes, combat logs and random-roll counts.
- **139 browser/HTTP checks passed**: real attacks and skills, target-local damage/effectiveness labels, one POST on double clicks, saved HP/SP reconciliation, turn-token rotation, guard, switching, healing, skipping, reduced motion, 390px layout, flee/victory restoration and ranked autoplay/manual replays. No browser script errors or failed game/asset HTTP responses occurred.
- **21 additional focused browser checks passed**: accepted commands bring offscreen battles into view, already-visible stages stay stationary, and three-hit skills show each value without overlapping popups. Single POST, rotated token and unlocked controls are preserved.
- **15 real MariaDB ranked checks passed**, covering human attacks/defenses, bot-only recording exclusion, final snapshots, duplicate rejection, party restoration and unchanged energy/reward settlement.
- **17 audio lifecycle checks passed** for permission handling, permitted cross-page resume, remembered mute, stale sound suppression, bounded voices/preloading, cancellation, volume and music/effect independence. All **228 shipped audio hashes** match the supplied ROM extraction; all ten battle-related audio files decode completely.
- Final real-page captures at 1440px, 720px and 390px have no horizontal overflow and all six sprites load. Compact stage heights are 652px, 677px and 689px respectively. Release and presentation validators pass, including PHP/JavaScript syntax, the complete species/move catalog and Dawn map integrity.

No-JavaScript fallback was checked using ordinary HTML form fields submitted over HTTP and then rendered in JavaScript-disabled Chromium. Native form clicks with JavaScript disabled are unavailable in this container's browser, including unchanged login. Audio tests verify source files, decoding and Web Audio lifecycle; they do not claim listening through physical speakers. Windows/WebView2 and Android device execution were not repeated in this server-only update.

Repeat the focused packaged checks with:

```text
php tools/test_battle_presentation.php
node tools/test_battle_audio.cjs
python tools/validate_release.py
python tools/validate_presentation.py
```

Evidence: `docs/verification/battle-animations-browser.json`, `battle-ranked-replays.json`, `battle-release-validation.txt`, representative battle screenshots and `docs/BATTLE_AUDIO_VERIFICATION.json`. Player-facing controls and update instructions are in `docs/BATTLE_PRESENTATION.md`.

Earlier release validation below is retained as historical evidence.

---

# v0.7.1 Sprite direction fix validation

The renderer now uses the existing sheet order down, right, left, up. All 192 horizontal frame pairs across all 64 Dawn tamer appearances are exact horizontal mirrors; no PNG assets were regenerated.

Twelve focused Chromium checks passed with the corrected game JavaScript and original CSS/PNG: left/right arrow keys, A/D, pointer direction buttons, unchanged up/down, reduced-motion and paused-animation modes. Each successful move selected the correct row and retained the facing on the idle frame. No browser script errors occurred. The rendered left/right Dawn Hero was visually inspected.

This browser check used controlled movement responses in a fixture; it did not repeat live database or Windows GUI tests. Existing movement, collision and server settlement logic is unchanged. JavaScript, PHP and changed Python source syntax checks passed. All 64 extraction metadata entries agree with the existing sheets. See docs/verification/sprite-direction-browser.json and sprite-direction-facing.png.

The v0.7.1 asset version changes browser script URLs and prompts native clients to refresh their asset manifests. Use Reload in Windows or reconnect Android after applying the server update. The four-file hotfix omits config/app.php and all database files.

Earlier release validation below is retained as historical evidence.

---

# v0.7.0 Dawn World validation

Built from the delivered v0.6.0 blank-password World Server release. The database remains schema 8 with 43 fresh-install tables. Validation ran on Linux with PHP 8.3.6, MariaDB 10.11.14 and Chromium. The separate Windows client is v1.1.0.

## Maps and original collision

- All **351 uploaded image layers** match the supplied attachment hashes. All **264 playable map images** retain their supplied dimensions; the 87 paired maps also pass exact alpha-composite pixel comparisons. Only atlas thumbnails are resized. The 11 old generated maps and their thumbnails/collision files are absent.
- Independently verified **4,690,560 movement cells against 75,055,104 original ROM collision bits**. A walkable cell contains no blocked original pixel. The map 088 canvas crop removes only blocked pixels.
- Every one of **481 landings** and **528 reciprocal gates** is on valid floor. All 264 maps are connected through the NXT travel graph. Original story event scripts are not ported; NXT landings provide access to separate paths and platforms.
- The final runtime sweep tested 480 combat landings plus the safe hub, produced 3,665 valid three-enemy squads, and exercised 264 actual bot visits. Spawn counts vary with available safe space. Tokens, roaming, wall/diagonal checks and claimed-spawn cooldowns remain authoritative. This run used 28 MiB peak PHP memory; it is a focused local check, not a concurrency or capacity guarantee.

## Gameplay, HTTP and upgrades

- **35 real HTTP checks passed** for authentication, CSRF, movement bounds/pacing, every intermediate movement cell, forged landings, gate corners, live encounters and party restoration. Attempts to travel during active combat return to the battle without moving the saved position.
- **48 checks passed** upgrading the exact v0.6.0 SQL baseline and repeating repair. Original data in 38 account, bot, ranking, console and progression tables was preserved. Legacy map rows remain intact, old links resolve safely, and all three renamed operation groups advance their existing event IDs/progress.
- Existing overworld authority, full 768-move team combat, database settlement/restoration, bot activation and exact v0.4/schema-6 upgrade regression suites passed. The bot activation run recorded genuine wild wins and losses, ranked matches and party changes.
- All three fresh SQL copies were regenerated from current migrations and seeds. Release and presentation validators passed, including PHP/JavaScript syntax, 1,000 species, 768 moves, 1,902 evolution routes and map integrity.

## Browser and Windows client

The Chromium journey passed **56 distinct checks** (171 including individual path segments), covering desktop/mobile atlas filtering, 24 lazy thumbnails with no eager full-map downloads, native image dimensions, cropped/tall/b-only maps, tamer scale, landings, gates and wall blocking. A real three-versus-three wild battle completed in 34 commands and restored all party HP/SP. **10 additional polish checks** passed for persisted quiet labels, touch movement, precise taps, held movement and stopping at walls. No browser script errors or failed game HTTP responses occurred. Reports and representative screenshots are in `docs/verification/`.

The separate Windows v1.1.0 client genuinely publishes as a self-contained AMD64 application with WebView2. **31 URL/origin tests and 22 local asset-cache tests passed**. All 3,984 bundled runtime assets match the server's advertised hashes/sizes/MIME types, including every playable map and thumbnail. Unused original layer copies are retained in the game source, excluded from the client bundle and never fetched unless requested. Manifest protocol and selected full-map download checks passed. Both client ZIPs passed integrity checks.

**Platform limit:** this Linux environment cannot execute the Windows GUI, WebView2 interaction, UAC or physical-session checks. Native Windows acceptance and sustained production-load testing remain necessary on the server/client machines. Compilation and portable tests do not claim those passes.

## Re-run and reports

```text
python tools/validate_dawn_assets.py
python tools/validate_dawn_world.py
python tools/validate_release.py
python tools/validate_presentation.py
php tools/test_dawn_world.php
php tools/test_dawn_upgrade.php
php tools/test_overworld.php
php tools/test_team_combat.php --database
php tools/test_bot_activation.php
php tools/test_v6_upgrade.php
```

Database suites create disposable test databases using the configured database connection. They require permission to create/drop those test databases and do not delete the configured save database. The source contains no test-generated world journals or native-manifest caches.

Main evidence: `docs/verification/dawn_integration_validation.json`, `dawn_http_security.json`, `dawn_rom_collision_validation.json`, `dawn_asset_validation.json`, `dawn-browser-journey.json` and `dawn-browser-polish.json`. Client build/cache/manifest reports are included in both separate Windows packages.

The validation below is retained historical evidence for earlier versions.

---

# v0.6.0 World Server console validation

Built from the working v0.5.1 **blank database password** release. This release uses schema 8. Tests ran on Linux with PHP 8.3.6, MariaDB 10.11.14 and PowerShell 7.6.2 for portable console components.

- **118 command assertions passed** across the 52-command catalog: real account creation, password hashing and resets, session revocation, roles, bans, balances, owned Digimon, party safeguards, inventory, scan, notices, maintenance, bot controls and redacted auditing. The suite creates its own disposable database.
- **84 CLI-to-HTTP integration checks passed**, including actual browser-session revocation, permission changes, bans, IP attribution, maintenance, announcement expiry/escaping, and denial of web access to the backend and journal. Saved map positions survive kicks and password resets.
- **12 backend protocol checks** and **5 persistent-worker policy checks passed**. A running worker observes pause/resume and maintenance changes without a process restart.
- **21 telemetry checks** and **8 independent journal security checks passed**, covering safe output, credential/query omission, actual connection addresses, committed-action records, rotation and bounded write handling.
- Migration from the blank-password baseline and repeated repair preserved all original columns and row hashes across **38 existing tables**. The older schema-5 and exact schema-6 fixture upgrade suites also passed against schema 8. Fresh SQL import passed; all three regenerated SQL copies contain 43 tables and no human/test accounts.
- **30 console component checks passed**: PowerShell parsing, C# compilation, UTF-8 PHP stdin, partial records, late-created files, rotation, traffic limits, masked-command history, honest grouped identities, journal-loss diagnostics, Apache game isolation and prompt-independent administration results. Independent review also compiled the backend's embedded Windows guard.
- The release validator passed catalog/assets/routes/schema checks and PHP/JavaScript syntax. The presentation validator passed. The two final player-facing release/privacy text changes passed PHP lint.

**Windows acceptance limitation:** this environment cannot execute the real Windows window, UAC relaunch, physical-session/WTS checks or Remote Desktop rejection. The launcher targets Windows PowerShell 5.1 and the built-in .NET Framework; those native Windows paths need acceptance testing on the server PC. Portable tests and source review do not establish a Windows end-to-end pass or a long-duration load guarantee.

Run `tools/start_world_server_windows.bat` on the physical server PC after Create / Repair Schema. Confirm `help` and `status`, visit the game to see activity while typing, and try a masked password prompt. The console opens no command-listening port. The existing autonomous tamer task remains separate.

Detailed reports: `docs/verification/world-console-security.json` and `docs/verification/world-console-components.txt`. Re-run the shipped focused suites with `php tools/test_world_commands.php`, `php tools/test_world_telemetry.php` and `powershell -NoProfile -ExecutionPolicy Bypass -File tools/world-console/test_console_components.ps1`.

The reports below describe earlier releases and retain their original version-specific results.

---

# v0.5.1 autonomous tamer fix validation

Tested on Linux PHP 8.3.6 / MariaDB 10.11.14 and Chromium 151. The Windows Task Scheduler installer was reviewed against Microsoft documentation, but **was not executed on Windows**. No long-duration capacity/load certification is claimed.

- `php tools/test_bot_activation.php`: fresh setup, no fabricated history, first browser-sized activation, interval throttling without heartbeat starvation, fair cursor, time budgets, shared worker lock, real ranked/scan accounting, restoration and preserving existing bot data on reseed. First 40 turns produced 28 wild battles (10 wins / 18 losses in this run), 8 ranked matches and 4 party changes.
- `php tools/test_ranked.php`: all 34 assertions passed again, including 1,000 distinct bots taking turns, genuine wild wins/losses, materialization, training, evolution, shopping, map presence, season rewards and replay/worker locks. Outcomes are simulated; ratios are not forced.
- Fresh browser installation created 1,000 bots and zero invented feed entries. Creating a player automatically processed the first 40 actions from the dashboard without launching a worker manually. Anonymous, GET and invalid-CSRF requests were rejected; an immediate valid repeat did zero work. Real wild and ranked records and nonzero leaderboard points appeared.
- Activity desktop (1440px) and mobile (390px) displayed actual feed entries without horizontal overflow or JavaScript errors. Ranked standings load their live-refresh script.
- **Zero-player operation:** closed the entire browser, removed human online presence and started the persistent worker. Bot progression continued. Stopped MySQL; the worker remained alive. Restarted MySQL; that same worker reconnected and resumed real actions without a player request. The separate daemon heartbeat was present.
- Full database data remains schema 7. The exact supplied schema-6 baseline upgrade, bot progress preservation and repeat repair are checked by `tools/test_v6_upgrade.php`. The three fresh-install SQL files are regenerated from current migrations/seeding.
- Release validation checks PHP/JavaScript syntax, routes, schemas, catalogs and assets. Browser screenshots and the detailed run report are in `docs/verification/`.

The startup task requires one local Windows installation step. It cannot keep a powered-off/asleep computer working; MySQL must be available, and the worker retries database outages. Browser-only fallback is deliberately distinct from persistent background operation. Continuous histories and collections need disk-space monitoring/backups on a long-running server.

The earlier gameplay/art/audio validation below documents v0.5.0; the focused checks above cover this scheduling fix.

---

# Dawn Link v0.5.0 validation

Tested against **PHP 8.3.6 + mysqli, MariaDB 10.11.14 and Chromium 151** on Linux. The package targets XAMPP, but Windows Apache/PHP deployment was not available in this session. The tests do not establish a production load or long-running service guarantee.

## Gameplay and persistence

- `php tools/test_team_combat.php`: all 768 move definitions, 3v3 initiative, individual and team targets, SP, damage, statuses, guard, reserve swaps, healing another partner, revival, buffs/cures and finite automated battles.
- `php tools/test_team_combat.php --database`: real database combat start/actions/settlement, stale command rejection, invalid-command rollback, scan secured once, escape/loss scan retention, duplicate encounter rejection, single reward settlement, team EXP and entire-party HP/SP restoration on win/loss/disconnect.
- `php tools/test_overworld.php`: all 11 maps, 80 reachable groups across 10 encounter routes, quiet hub, legal roaming, stable tokens on refresh, range/wall checks, owner/sector validation, expired/consumed signal rejection and respawn cooldown.
- `php tools/test_ranked.php`: persistent rival seeding, actual owned teams, ranked simulation/records, energy, foreign/repeated ticket rejection, bot actions and outcomes, materialization, rotation, training, evolution, medals, rewards and worker overlap protection. 34 assertions passed. A complete cursor rotation processed all 1,000 distinct bots without errors, recording 321 genuine wild wins and 679 losses. Shop spending, legal map presence and frozen historical ranks also passed.

## Baseline regression and installation

Fresh SQL was rebuilt from the real schema/seed functions into three byte-identical copies. All 39 tables and the 1,000 AI accounts are included. The broad gameplay suite passed across all 1,000 forms, 768 moves and 1,902 evolution routes, including materialization, ABI, DNA, training, inventory receipts and ownership-preserving exchange.

The upgrade suite passed revision 5 → 7 with original species/instance IDs, names, levels, wallet, scan, inventory, party links and proportional older-vitals/EXP migration preserved. Repeating repair was idempotent. Retiring a legacy battle restored its party without creating a reward. A fresh SQL import passed and contained no human/test accounts.

The exact supplied v0.4/schema-6 SQL was retained byte-for-byte as `tools/fixtures/schema_v6.sql`. `php tools/test_v6_upgrade.php` passed schema 6 → 7 while preserving account/password, all original 1,000 species IDs, nickname, ABI/CAM, training, level/EXP, learned moves, party/reserve, wallet, items, scans, map position and old arena records. Legacy active combat retired with full party restoration and no reward; actual bot activity survived another repair without duplicate accounts.

## Real browser gameplay

A real account completed setup and signup, received three partners, selected Dawn Hero, saved the choice, moved on the map, selected a visible roaming group, and finished a real 3v3 wild battle. All six allied HP/SP bars returned to their maxima. A real ranked match also settled successfully.

Audio decoded and played in the browser. All 46 music entries were available; music volume, mute and page-reload persistence worked. An 80-tamer worker batch completed without errors and populated the visible activity screen.

Seventeen desktop/mobile captures at 1440px and 390px showed **no horizontal overflow, missing images, browser exceptions, failed requests or HTTP errors**. Selected captures and the structured report are in `docs/verification/`. Signup/options exposed all 66 choices: 64 Dawn appearances and two retained avatars. Native Dawn walking sheets rendered on the map. A targeted follow-up also passed with a bounded 540px leaderboard and eight visible AI-labeled map tamers; real-player map presence sorts ahead of AI presence.

Health returned `ok:true`, version `0.5.0`, schema `7`, 39 tables, 1,000 species, 1,000 bots, 64 Dawn avatars and 46 music tracks.

## Asset integrity

- Every one of the 2,201 extracted PNGs opened successfully. All 361 integrated Dawn portraits are nonempty 256×256 transparent PNGs. The 64 tamer IDs and 3×4 directional sheets validated.
- All 228 delivered Ogg files matched their recorded SHA-256 and fully decoded through ffmpeg. All 46 original BGM sequences are included, together with 182 playable effects. The source-only `se024` zero-time-loop effect is documented in `docs/DAWN_AUDIO.md`.
- Graphical mappings use internal ROM species IDs and exact roster names/aliases, not inferred lookalikes. Map layers and UI sheets are decoded assets; the original Dawn map event/collision systems are not ported.

## Re-run

Use Python 3 + Pillow and PHP with mysqli:

```text
python tools/validate_release.py
python tools/validate_presentation.py
python tools/validate_dawn_audio.py
php tools/test_gameplay.php
php tools/test_upgrade.php
php tools/test_v6_upgrade.php
php tools/test_team_combat.php --database
php tools/test_overworld.php
php tools/test_ranked.php
```

PHP database tests create and delete newly named test databases; they do not delete the configured game-save database. The database account must be allowed to create test databases. Historical v0.3/v0.4 reports are explicitly retained under `docs/HISTORICAL_*`.
