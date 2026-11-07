(() => {
  'use strict';

  const cfg = window.PV_WORLD_MAP_CONFIG;
  if (!cfg) return;

  const stage = document.getElementById('pv-world-map-stage');
  const viewport = document.getElementById('pv-world-map-viewport');
  const actors = document.getElementById('pv-world-map-actors');
  const encounter = document.getElementById('pv-world-map-encounter');
  const wildLayer = document.getElementById('dn-world-wild');
  const nearby = document.getElementById('dn-nearby-signals');
  const status = document.getElementById('pv-world-map-status');
  const coordinates = document.getElementById('pv-world-coordinates');
  const mapArt = document.getElementById('pv-world-map-art');
  const buttons = Array.from(document.querySelectorAll('[data-world-direction]'));
  if (!stage || !viewport || !actors || !status || !coordinates) return;

  const state = {
    x: Number(cfg.x) || 1,
    y: Number(cfg.y) || 1,
    tamer: Number(cfg.tamer) || 1,
    tile: Math.max(8, Number(cfg.tileSize) || 33),
    logicalTile: Math.max(8, Number(cfg.logicalTileSize) || 16),
    players: Array.isArray(cfg.players) ? cfg.players : [],
    blocked: Array.isArray(cfg.blockedDirections) ? cfg.blockedDirections.map(Number) : [],
    moving: false,
    engaging: false,
    selected: null,
    detailKey: null,
    facing: 'down',
    walkFrame: 0,
    wild: Array.isArray(cfg.wild) ? cfg.wild : [],
  };

  const esc = value => String(value ?? '').replace(/[&<>'"]/g, ch => ({
    '&': '&amp;', '<': '&lt;', '>': '&gt;', "'": '&#39;', '"': '&quot;',
  }[ch]));

  function sprite(tamer, own = false) {
    const id = Number(tamer) || 1;
    const walk = (cfg.tamerWalk || {})[id];
    if (walk) {
      const row = { down: 0, left: 1, right: 2, up: 3 }[own ? state.facing : 'down'];
      return `<span class="dn-map-avatar dn-sheet-avatar" aria-hidden="true" style="background-image:url('${esc(walk.sheet)}');background-position:${own ? state.walkFrame * 50 : 0}% ${row * 100 / 3}%"></span>`;
    }
    const url = (cfg.tamerSprites || {})[id] || (cfg.tamerSprites || {})[1] || `${cfg.tamerBase}1.png`;
    return `<img class="dn-map-avatar" src="${esc(url)}" alt="">`;
  }

  const signalDistance = signal => Math.max(Math.abs(Number(signal.x) - state.x), Math.abs(Number(signal.y) - state.y));
  function chooseSignal(id) {
    state.selected = id;
    renderWild();
    const signal = state.wild.find(item => item.id === id);
    if (signal) setStatus(`${signal.name} · Lv. ${signal.level}. ${signal.nearby ? 'In range. Choose Battle to begin.' : 'Move closer to battle this squad.'}`);
  }

  function renderWild() {
    if (!wildLayer || !nearby) return;
    const focus = document.activeElement;
    const focusId = focus?.dataset?.signalId;
    const focusLayer = focusId ? (wildLayer.contains(focus) ? wildLayer : nearby) : null;
    const visible = new Set(state.wild.map(item => String(item.id)));
    for (const button of wildLayer.querySelectorAll('[data-signal-id]')) {
      if (!visible.has(button.dataset.signalId)) button.remove();
    }
    state.wild.forEach(signal => {
      let button = wildLayer.querySelector(`[data-signal-id="${signal.id}"]`);
      if (!button) {
        button = document.createElement('button');
        button.type = 'button';
        button.dataset.signalId = signal.id;
        button.className = 'dn-roaming-digimon';
        button.innerHTML = `<img src="${esc(signal.sprite)}" alt="" draggable="false"><span><b>${esc(signal.name)}</b><small>Lv. ${Number(signal.level)} · ×${Number(signal.count) || 3}</small></span>`;
        wildLayer.appendChild(button);
      }
      button.style.left = `${(Number(signal.x) - 0.5) * state.tile}px`;
      button.style.top = `${(Number(signal.y) - 0.5) * state.tile}px`;
      button.classList.toggle('is-nearby', Boolean(signal.nearby));
      button.classList.toggle('is-selected', signal.id === state.selected);
      button.setAttribute('aria-pressed', signal.id === state.selected ? 'true' : 'false');
      button.setAttribute('aria-label', `${signal.name}, level ${signal.level}, squad of ${signal.count || 3}. ${signal.nearby ? 'In battle range' : `${signalDistance(signal)} tiles away`}. Select signal.`);
    });
    const sorted = [...state.wild].sort((a, b) => signalDistance(a) - signalDistance(b));
    nearby.innerHTML = sorted.map(signal => `<button type="button" class="dn-signal-row ${signal.id === state.selected ? 'is-selected' : ''}" data-signal-id="${esc(signal.id)}" aria-pressed="${signal.id === state.selected}"><img src="${esc(signal.sprite)}" alt="" width="44" height="44"><span><strong>${esc(signal.name)}</strong><small>Lv. ${Number(signal.level)} · ${signal.nearby ? 'In range' : `${signalDistance(signal)} tiles away`}</small></span><b aria-hidden="true">${signal.nearby ? '◎' : '›'}</b></button>`).join('');
    if (focusId && focusLayer === nearby) nearby.querySelector(`[data-signal-id="${focusId}"]`)?.focus({ preventScroll: true });
    const selected = state.wild.find(item => item.id === state.selected);
    if (state.selected && !selected) {
      state.selected = null;
      state.detailKey = null;
      encounter.innerHTML = '<div class="pv-map-quiet"><strong>Signal cleared.</strong><span>Choose another roaming Digimon. New signals return after a short delay.</span></div>';
    }
    const detailKey = selected ? JSON.stringify([selected.id, selected.x, selected.y, selected.nearby, state.engaging]) : null;
    if (selected && encounter && detailKey !== state.detailKey) {
      state.detailKey = detailKey;
      const launchFocused = document.activeElement?.id === 'dn-engage-signal';
      encounter.innerHTML = `<div class="dn-signal-detail"><img src="${esc(selected.sprite)}" alt="${esc(selected.name)}"><small>${esc(selected.stage)} · WILD SQUAD</small><strong>${esc(selected.name)}</strong><span>Level ${Number(selected.level)} · ${Number(selected.count) || 3} opponents</span><p>${selected.nearby ? 'Scan data is secured on contact. Win the battle to earn EXP and Bits.' : `Signal at X ${Number(selected.x)} / Y ${Number(selected.y)}. Move within two clear steps to battle.`}</p><button class="pv-button" id="dn-engage-signal" type="button" ${!selected.nearby || state.engaging ? 'disabled' : ''}>${state.engaging ? 'Connecting…' : 'Battle + Scan'}</button><small>Full party restoration after every battle</small></div>`;
      if (launchFocused) document.getElementById('dn-engage-signal')?.focus({ preventScroll: true });
    }
  }

  async function engage() {
    if (state.moving || state.engaging || !state.selected) return;
    state.engaging = true;
    renderWild();
    setStatus('Connecting to the selected wild squad…', 'busy');
    try {
      const body = new URLSearchParams({ spawn_id: state.selected, area: cfg.area, csrf_token: cfg.csrf });
      const response = await fetch(cfg.encounterUrl, { method: 'POST', credentials: 'same-origin', headers: { 'Content-Type': 'application/x-www-form-urlencoded;charset=UTF-8' }, body });
      const data = await response.json();
      if (!response.ok || !data.ok) throw new Error(data.error || 'The signal could not be locked.');
      if (data.redirect) { window.location.href = data.redirect; return; }
      const form = document.createElement('form');
      form.method = 'post';
      form.action = data.action;
      for (const [name, value] of Object.entries({ start_wild_battle: '1', encounter_token: data.encounterToken, csrf_token: data.csrf })) {
        const input = document.createElement('input'); input.type = 'hidden'; input.name = name; input.value = value; form.appendChild(input);
      }
      document.body.appendChild(form);
      form.submit();
    } catch (error) {
      setStatus(error.message || 'The scanner is reconnecting. Try again.', 'error');
      state.engaging = false;
      await presence();
      renderWild();
    }
  }

  function actor(player, own = false) {
    const x = Number(player.x) || 1;
    const y = Number(player.y) || 1;
    const ratio = Math.max(1, state.tile / state.logicalTile);
    const unit = Math.round(16 * ratio);
    const left = (x - 1) * state.tile + Math.max(0, (state.tile - unit) / 2);
    const tamer = own ? state.tamer : Number(player.tamer);
    const walk = (cfg.tamerWalk || {})[tamer];
    const height = walk ? Math.round(unit * Number(walk.height) / Math.max(1, Number(walk.width))) : unit;
    const top = walk ? y * state.tile - height : (y - 1) * state.tile - unit;
    const name = own ? 'You' : (player.is_bot ? 'AI · ' : '') + esc(player.username || 'Tamer');
    return `<span class="pv-map-actor${own ? ' pv-map-actor-own' : ''}${walk ? ' dn-dawn-map-actor' : ''}" style="left:${left}px;top:${top}px;--pv-world-actor-unit:${unit}px;--pv-world-actor-height:${height}px" title="${name}">${sprite(tamer, own)}<b>${name}</b></span>`;
  }

  function render() {
    let html = actor({ x: state.x, y: state.y }, true);
    state.players.forEach(player => {
      const x = Number(player.x);
      const y = Number(player.y);
      if (x >= 1 && x <= Number(cfg.columns) && y >= 1 && y <= Number(cfg.rows)) {
        html += actor(player, false);
      }
    });
    actors.innerHTML = html;
    coordinates.textContent = `X ${state.x} // Y ${state.y}`;
    renderWild();
  }

  function refreshButtons() {
    buttons.forEach(button => {
      const direction = Number(button.dataset.worldDirection);
      const isBlocked = state.blocked.includes(direction);
      button.disabled = state.moving || state.engaging || isBlocked;
      button.classList.toggle('is-path-blocked', isBlocked);
      button.setAttribute('aria-disabled', isBlocked ? 'true' : 'false');
    });
  }

  function setBusy(value) {
    state.moving = value;
    refreshButtons();
    stage.classList.toggle('is-moving', value);
  }

  function setStatus(message, tone = '') {
    status.textContent = message;
    status.dataset.tone = tone;
  }

  function clamp(value, minimum, maximum) {
    return Math.min(Math.max(value, minimum), Math.max(minimum, maximum));
  }

  /*
   * Native-map tracked camera contract
   * ----------------------------------
   * This preserves the accepted predecessor regional-map camera contract: the
   * artwork remains at its natural size and only the scroll viewport moves. Camera coordinates use the stage's real origin in
   * the scrollport, preventing browser scroll anchoring/restoration from
   * drifting the tamer away from center after movement or responsive layout.
   */
  function cameraTarget() {
    const viewportRect = viewport.getBoundingClientRect();
    const stageRect = stage.getBoundingClientRect();
    const stageLeft = stageRect.left - viewportRect.left + viewport.scrollLeft;
    const stageTop = stageRect.top - viewportRect.top + viewport.scrollTop;
    const playerX = stageLeft + (state.x - 0.5) * state.tile;
    const playerY = stageTop + (state.y - 0.5) * state.tile;
    const maxLeft = Math.max(0, viewport.scrollWidth - viewport.clientWidth);
    const maxTop = Math.max(0, viewport.scrollHeight - viewport.clientHeight);

    return {
      left: clamp(Math.round(playerX - viewport.clientWidth / 2), 0, maxLeft),
      top: clamp(Math.round(playerY - viewport.clientHeight / 2), 0, maxTop),
    };
  }

  function centerCamera() {
    const target = cameraTarget();
    const previousInlineBehavior = viewport.style.scrollBehavior;
    viewport.style.scrollBehavior = 'auto';
    try {
      if (typeof viewport.scrollTo === 'function') {
        viewport.scrollTo({ left: target.left, top: target.top, behavior: 'instant' });
      } else {
        viewport.scrollLeft = target.left;
        viewport.scrollTop = target.top;
      }
    } catch (error) {
      viewport.scrollLeft = target.left;
      viewport.scrollTop = target.top;
    } finally {
      viewport.style.scrollBehavior = previousInlineBehavior;
    }
  }

  let cameraFrame = 0;
  function scheduleCamera() {
    if (cameraFrame) window.cancelAnimationFrame(cameraFrame);
    cameraFrame = window.requestAnimationFrame(() => {
      cameraFrame = window.requestAnimationFrame(() => {
        cameraFrame = 0;
        centerCamera();
      });
    });
  }

  let walkTimer = 0;
  let walkStopTimer = 0;
  function drawWalkFrame() {
    const avatar = actors.querySelector('.pv-map-actor-own .dn-sheet-avatar');
    if (avatar) avatar.style.backgroundPositionX = `${state.walkFrame * 50}%`;
  }
  function stopWalking() {
    window.clearInterval(walkTimer); window.clearTimeout(walkStopTimer);
    walkTimer = 0; walkStopTimer = 0; state.walkFrame = 0; drawWalkFrame();
  }
  function startWalking(direction) {
    state.facing = [1, 5, 7].includes(direction) ? 'up' : [2, 6, 8].includes(direction) ? 'down' : direction === 3 ? 'left' : 'right';
    if (document.body.classList.contains('nxt-motion-paused') || window.matchMedia('(prefers-reduced-motion: reduce)').matches) { stopWalking(); return; }
    if (!walkTimer) {
      state.walkFrame = 1;
      walkTimer = window.setInterval(() => { state.walkFrame = (state.walkFrame + 1) % 3; drawWalkFrame(); }, 140);
    }
    window.clearTimeout(walkStopTimer);
    walkStopTimer = window.setTimeout(stopWalking, 330);
  }

  async function move(direction) {
    if (state.moving || state.engaging) return;
    setBusy(true);
    setStatus('Validating network path…', 'busy');

    try {
      const body = new URLSearchParams();
      body.set('direction', String(direction));
      body.set('area', String(cfg.area));
      body.set('csrf_token', String(cfg.csrf || ''));

      const response = await fetch(cfg.moveUrl, {
        method: 'POST',
        credentials: 'same-origin',
        headers: {
          'Content-Type': 'application/x-www-form-urlencoded;charset=UTF-8',
          'X-Requested-With': 'XMLHttpRequest',
        },
        body: body.toString(),
      });

      if (response.status === 401) {
        window.location.href = `${cfg.base}/login.php?expired=1`;
        return;
      }
      if (response.status === 429) {
        setStatus('Movement input received too quickly. Ready.', 'notice');
        return;
      }

      const data = await response.json();
      if (!response.ok || !data.ok) throw new Error(data.error || `HTTP ${response.status}`);
      if (data.redirect) {
        setStatus('Transferring to connected sector…', 'success');
        window.location.href = data.redirect;
        return;
      }
      if (!data.moved) {
        if (Array.isArray(data.blockedDirections)) state.blocked = data.blockedDirections.map(Number);
        refreshButtons();
        setStatus('Collision boundary: that route is blocked.', 'blocked');
        stage.classList.add('is-blocked');
        window.setTimeout(() => stage.classList.remove('is-blocked'), 180);
        return;
      }

      startWalking(direction);
      state.x = Number(data.x) || state.x;
      state.y = Number(data.y) || state.y;
      state.players = Array.isArray(data.players) ? data.players : state.players;
      state.wild = Array.isArray(data.wild) ? data.wild : state.wild;
      if (Array.isArray(data.blockedDirections)) state.blocked = data.blockedDirections.map(Number);
      render();
      refreshButtons();
      centerCamera();
      scheduleCamera();
      setStatus('Network movement confirmed.', 'success');
    } catch (error) {
      console.error('Cyberspace movement failed', error);
      setStatus('The network link did not respond. Try again.', 'error');
    } finally {
      setBusy(false);
    }
  }

  const keys = new Map([
    ['ArrowUp', 1], ['w', 1], ['W', 1], ['8', 1],
    ['ArrowDown', 2], ['s', 2], ['S', 2], ['2', 2],
    ['ArrowLeft', 3], ['a', 3], ['A', 3], ['4', 3],
    ['ArrowRight', 4], ['d', 4], ['D', 4], ['6', 4],
    ['7', 5], ['1', 6], ['9', 7], ['3', 8],
  ]);

  let presenceActive = false;
  async function presence() {
    if (presenceActive || state.moving || state.engaging || document.visibilityState !== 'visible') return;
    presenceActive = true;
    try {
      const response = await fetch(String(cfg.presenceUrl || ''), {
        method: 'GET',
        credentials: 'same-origin',
        cache: 'no-store',
        headers: { 'X-Requested-With': 'XMLHttpRequest' },
      });
      if (response.status === 401) return;
      const data = await response.json();
      if (!response.ok || !data.ok || String(data.world) !== String(cfg.world) || String(data.area) !== String(cfg.area)) return;
      state.players = Array.isArray(data.players) ? data.players : [];
      state.wild = Array.isArray(data.wild) ? data.wild : state.wild;
      render();
    } catch (error) {
      console.debug('Cyberspace presence refresh skipped', error);
    } finally {
      presenceActive = false;
    }
  }

  [wildLayer, nearby].filter(Boolean).forEach(layer => layer.addEventListener('click', event => {
    const button = event.target.closest('[data-signal-id]');
    if (button) chooseSignal(button.dataset.signalId);
  }));
  encounter?.addEventListener('click', event => { if (event.target.closest('#dn-engage-signal')) engage(); });

  buttons.forEach(button => button.addEventListener('click', () => move(Number(button.dataset.worldDirection))));
  document.addEventListener('keydown', event => {
    const target = event.target;
    if (target && /^(INPUT|TEXTAREA|SELECT)$/.test(target.tagName)) return;
    if (event.key.toLowerCase() === 'e') {
      event.preventDefault();
      const closest = [...state.wild].sort((a, b) => signalDistance(a) - signalDistance(b))[0];
      if (closest) { chooseSignal(closest.id); document.getElementById('dn-engage-signal')?.focus({ preventScroll: true }); }
      return;
    }
    const direction = keys.get(event.key);
    if (!direction) return;
    event.preventDefault();
    move(direction);
  });

  document.addEventListener('visibilitychange', () => {
    if (document.visibilityState !== 'visible') stopWalking();
    if (document.visibilityState === 'visible') {
      presence();
      scheduleCamera();
    }
  });
  window.addEventListener('resize', scheduleCamera, { passive: true });
  window.addEventListener('load', scheduleCamera, { once: true });
  window.addEventListener('pageshow', scheduleCamera);

  if (typeof ResizeObserver === 'function') {
    const cameraResizeObserver = new ResizeObserver(scheduleCamera);
    cameraResizeObserver.observe(viewport);
  }

  if (document.fonts && document.fonts.ready && typeof document.fonts.ready.then === 'function') {
    document.fonts.ready.then(scheduleCamera).catch(() => {});
  }

  if (mapArt) {
    mapArt.addEventListener('load', scheduleCamera);
    mapArt.addEventListener('error', () => setStatus('Sector artwork could not be rendered. Return to the network sector list.', 'error'), { once: true });
    if (mapArt.complete && typeof mapArt.decode === 'function') {
      mapArt.decode().then(scheduleCamera).catch(scheduleCamera);
    }
  }

  render();
  refreshButtons();
  centerCamera();
  scheduleCamera();
  window.setInterval(presence, Math.max(1200, Math.min(10000, Number(cfg.presenceInterval) || 2000)));
})();
