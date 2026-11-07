<?php
declare(strict_types=1);

if (!function_exists('pv_url')) {
    http_response_code(404);
    exit;
}

$clientReleases = require __DIR__.'/../config/client_downloads.php';
$windowsClient = $clientReleases['windows'];
$clientUrl = trim((string)($windowsClient['url'] ?? ''));
$clientUrlParts = parse_url($clientUrl);
// Link directly to the external host. Never fetch, proxy, cache or serve the ZIP here.
$clientAvailable = filter_var($clientUrl, FILTER_VALIDATE_URL) !== false
    && is_array($clientUrlParts)
    && strtolower((string)($clientUrlParts['scheme'] ?? '')) === 'https'
    && !empty($clientUrlParts['host'])
    && !isset($clientUrlParts['user']) && !isset($clientUrlParts['pass']);
$clientSize = number_format((int)$windowsClient['bytes'] / 1048576, 0).' MiB';
$browserDestination = pv_is_logged_in() ? 'dashboard.php' : 'login.php';
?>
<link rel="stylesheet" href="<?=pv_h(pv_asset('css/client-download.css'))?>?v=1">
<section class="nxt-client-card" id="download-client" aria-labelledby="nxt-client-title">
    <div class="nxt-client-copy">
        <span class="pv-eyebrow">PLAY YOUR WAY</span>
        <h2 id="nxt-client-title">Take NXT to your desktop.</h2>
        <p>Prefer a dedicated game window? Connect to the same server with your existing tamer and keep your adventure going.</p>
        <ul class="nxt-client-features" aria-label="Client features">
            <li><?=nxt_icon('scan')?><span><strong>A clearer view</strong>200% default zoom</span></li>
            <li><?=nxt_icon('partner')?><span><strong>Built-in Dawn assets</strong>Updates as assets change</span></li>
            <li><?=nxt_icon('settings')?><span><strong>Your server, your choice</strong>IP addresses &amp; domains</span></li>
        </ul>
    </div>
    <div class="nxt-client-download">
        <div class="nxt-client-platform">
            <svg viewBox="0 0 24 24" fill="currentColor" aria-hidden="true"><path d="M2 2h9v9H2zm11 0h9v9h-9zM2 13h9v9H2zm11 0h9v9h-9z"/></svg>
            <div><h3>Windows x64 client</h3><p>v<?=pv_h((string)$windowsClient['version'])?> <span aria-hidden="true">·</span> <?=pv_h($clientSize)?> ZIP</p></div>
        </div>
        <?php if ($clientAvailable): ?>
        <a class="pv-button nxt-client-download-button" href="<?=pv_h($clientUrl)?>" target="_blank" rel="noopener noreferrer" aria-describedby="nxt-client-requirements nxt-client-external-note">
            <svg class="nxt-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M12 3v12m-5-5 5 5 5-5M4 16v5h16v-5"/></svg>
            Download for Windows
        </a>
        <p class="nxt-client-requirements" id="nxt-client-external-note">Opens the external download in a new tab.</p>
        <?php else: ?>
        <p class="nxt-client-unavailable" role="status">The client download is currently unavailable. You can still play in your browser.</p>
        <?php endif; ?>
        <p class="nxt-client-requirements" id="nxt-client-requirements">Windows 10 / 11 · 64-bit<br>Powered by Microsoft Edge WebView2</p>
        <a class="nxt-client-browser" href="<?=pv_h(pv_url($browserDestination))?>">Play in browser <?=nxt_icon('next')?></a>
    </div>
    <details class="nxt-client-help">
        <summary>First launch &amp; server setup <span aria-hidden="true">+</span></summary>
        <div class="nxt-client-help-content">
            <ol>
                <li><strong>Extract the entire ZIP.</strong> Keep its files together in a folder on your PC.</li>
                <li><strong>Open DigimonNXT.exe.</strong> If WebView2 is missing, run the included <b>Install WebView2.cmd</b> with an internet connection.</li>
                <li><strong>Choose Server &amp; options.</strong> Enter this game website’s address, including its game folder, then connect and log in.</li>
            </ol>
            <p>The default server is <code>http://127.0.0.1/DigimonNXT/</code>, for a game hosted on your own PC. To play on another server, enter its IP address or domain. Your tamer and saves belong to the server you choose; an active server connection is required.</p>
        </div>
    </details>
</section>
