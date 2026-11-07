/* Genuine SDAT music and effects; a single looping music buffer, bounded SFX cache. */
(() => {
  'use strict';
  const config = window.DAWN_AUDIO_CONFIG;
  if (!config || !config.tracks || window.DawnAudio) return;
  const read = (key, fallback) => { try { const value = localStorage.getItem('nxt.dawn.' + key); return value === null ? fallback : value; } catch (_) { return fallback; } };
  const save = (key, value) => { try { localStorage.setItem('nxt.dawn.' + key, String(value)); } catch (_) {} };
  const clamp = value => Math.max(0, Math.min(1, Number.isFinite(+value) ? +value : .4));
  let enabled = read('enabled', 'true') === 'true';
  let musicVolume = clamp(read('musicVolume', '.38')), effectsVolume = clamp(read('effectsVolume', '.55'));
  let context, musicGain, effectsGain, source, currentId = '', musicBuffer = null, startedAt = 0, startOffset = 0;
  let requestedId = '', changeToken = 0, unlocked = false, inFlight, suspendedByVisibility = false;
  let selected = 'auto'; // Track overrides belong to this page; normal scene changes remain automatic.
  const effectsCache = new Map(), pendingEffects = new Map(), lastEffectAt = new Map(), activeEffects = new Set();
  const status = document.querySelector('[data-dawn-status]'), toggle = document.querySelector('[data-dawn-toggle]');
  const setStatus = text => { if (status) status.textContent = text; };
  const update = () => {
    if (toggle) { toggle.textContent = enabled && unlocked ? 'Mute sound' : 'Enable sound'; toggle.setAttribute('aria-pressed', String(enabled && unlocked)); }
    if (!enabled) setStatus('Sound is muted.');
  };
  const setup = () => {
    if (context) return true;
    const Constructor = window.AudioContext || window.webkitAudioContext;
    if (!Constructor) { setStatus('Your browser does not support this audio player.'); return false; }
    context = new Constructor();
    musicGain = context.createGain(); effectsGain = context.createGain();
    musicGain.gain.value = musicVolume; effectsGain.gain.value = effectsVolume;
    musicGain.connect(context.destination); effectsGain.connect(context.destination);
    return true;
  };
  const stopMusic = () => { if (source) { try { source.stop(); } catch (_) {} source.disconnect(); source = null; } };
  const decode = async id => {
    const track = config.tracks[id];
    if (!track) throw new Error('Track is unavailable');
    const response = await fetch(track.url, { cache: 'force-cache', credentials: 'same-origin' });
    if (!response.ok) throw new Error('Track could not be loaded');
    return context.decodeAudioData(await response.arrayBuffer());
  };
  const music = async id => {
    if (!enabled || !unlocked || !config.tracks[id]) return;
    if (requestedId === id && (source || inFlight)) return;
    requestedId = id;
    const token = ++changeToken;
    setStatus('Loading Dawn music…');
    try {
      inFlight = currentId === id && musicBuffer ? Promise.resolve(musicBuffer) : decode(id);
      const buffer = await inFlight;
      if (token !== changeToken || !enabled) return;
      stopMusic(); musicBuffer = buffer; currentId = id;
      source = context.createBufferSource(); source.buffer = buffer; source.connect(musicGain);
      const entry = config.tracks[id];
      if (entry.loopEnd > entry.loopStart) { source.loop = true; source.loopStart = entry.loopStart; source.loopEnd = Math.min(entry.loopEnd, buffer.duration); }
      let offset = 0;
      try {
        const previous = JSON.parse(sessionStorage.getItem('nxt.dawn.position') || 'null');
        if (previous && previous.id === id && Date.now() - previous.saved < 1800000) offset = Math.max(0, Math.min(+previous.offset || 0, buffer.duration - .1));
      } catch (_) {}
      startedAt = context.currentTime; startOffset = offset;
      source.start(0, offset);
      source.onended = () => { if (!source?.loop && enabled) setStatus('Dawn · ' + id.toUpperCase()); };
      setStatus('Playing Dawn · ' + id.toUpperCase()); update();
    } catch (_) { if (token === changeToken) { requestedId = ''; setStatus('Music could not load. Click Enable sound to retry.'); unlocked = false; update(); } }
    finally { if (token === changeToken) inFlight = null; }
  };
  const unlock = async () => {
    if (!enabled || !setup()) return;
    try { await context.resume(); unlocked = context.state === 'running'; } catch (_) { unlocked = false; }
    update();
    if (unlocked) music(selected === 'auto' ? config.track : selected);
  };
  const effect = async name => {
    if (!enabled || !unlocked || effectsVolume === 0) return;
    const id = config.effects[name] || name;
    if (!config.tracks[id]) return;
    const now = performance.now();
    if (now - (lastEffectAt.get(id) || -1000) < 70) return;
    lastEffectAt.set(id, now);
    try {
      let buffer = effectsCache.get(id);
      if (!buffer) {
        if (!pendingEffects.has(id)) pendingEffects.set(id, decode(id).finally(() => pendingEffects.delete(id)));
        buffer = await pendingEffects.get(id);
        effectsCache.set(id, buffer);
        if (effectsCache.size > 12) effectsCache.delete(effectsCache.keys().next().value);
      }
      if (!enabled) return;
      if (activeEffects.size >= 8) { const oldest = activeEffects.values().next().value; try { oldest.stop(); } catch (_) {} activeEffects.delete(oldest); }
      const node = context.createBufferSource(); node.buffer = buffer; node.connect(effectsGain); activeEffects.add(node);
      node.onended = () => { node.disconnect(); activeEffects.delete(node); }; node.start();
    } catch (_) { /* A failed effect must never interrupt gameplay. */ }
  };
  const scene = name => { if (config.scenes[name]) { config.track = config.scenes[name]; if (selected === 'auto') music(config.track); } };
  window.DawnAudio = Object.freeze({ effect, scene, play: music, unlock });
  toggle?.addEventListener('click', async () => {
    enabled = !enabled || !unlocked; save('enabled', enabled);
    if (enabled) await unlock();
    else { ++changeToken; requestedId = ''; inFlight = null; stopMusic(); if (context) await context.suspend(); }
    update();
  });
  [['[data-dawn-music-volume]', 'musicVolume'], ['[data-dawn-effects-volume]', 'effectsVolume']].forEach(([selector, key]) => {
    const input = document.querySelector(selector); if (!input) return;
    input.value = Math.round((key === 'musicVolume' ? musicVolume : effectsVolume) * 100);
    input.addEventListener('input', () => {
      const volume = clamp(input.value / 100); save(key, volume);
      if (key === 'musicVolume') { musicVolume = volume; if (musicGain) musicGain.gain.value = volume; }
      else { effectsVolume = volume; if (effectsGain) effectsGain.gain.value = volume; }
    });
  });
  document.querySelector('[data-dawn-track]')?.addEventListener('change', async event => { selected = event.target.value; enabled = true; save('enabled', true); await unlock(); });
  document.addEventListener('pointerdown', event => { if (!event.target.closest('[data-dawn-toggle]') && enabled && !unlocked) unlock(); }, { passive: true });
  document.addEventListener('keydown', () => { if (enabled && !unlocked) unlock(); }, { passive: true });
  document.addEventListener('click', event => {
    const control = event.target.closest('[data-dawn-sfx],button,a[href],summary');
    if (!control || control.closest('.dawn-audio') || control.hasAttribute('disabled')) return;
    effect(control.dataset.dawnSfx || (control.matches('button') ? 'confirm' : 'select'));
  }, { passive: true });
  document.addEventListener('nxt:audio', event => { if (event.detail?.effect) effect(event.detail.effect); if (event.detail?.scene) scene(event.detail.scene); });
  const remember = () => {
    if (!source || !context || !currentId) return;
    let offset = startOffset + context.currentTime - startedAt;
    const track = config.tracks[currentId];
    if (source.loop && offset >= track.loopEnd) offset = track.loopStart + ((offset - track.loopStart) % (track.loopEnd - track.loopStart));
    try { sessionStorage.setItem('nxt.dawn.position', JSON.stringify({ id: currentId, offset, saved: Date.now() })); } catch (_) {}
  };
  window.addEventListener('pagehide', remember);
  document.addEventListener('visibilitychange', async () => {
    if (!context || !enabled) return;
    if (document.hidden) { remember(); suspendedByVisibility = true; await context.suspend(); }
    else if (suspendedByVisibility) { suspendedByVisibility = false; await unlock(); }
  });
  const result = document.querySelector('[data-dawn-result]');
  if (result) config.track = config.scenes[result.dataset.dawnResult === 'win' ? 'victory' : 'defeat'];
  update();
  // Browsers may permit resuming on subsequent pages after the first gesture.
  if (enabled) unlock();
})();
