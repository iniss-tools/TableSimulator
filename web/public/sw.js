// Service worker simulatora: rozhranie (stranka a subory /assets) z vyrovnavacej pamate, ked server nebezi - kiosk
// sa nacita aj pocas restartu servera alebo po zapnuti zariadenia skor nez server a pripoji sa sam. API, WebSocket,
// vzhlady a pisma idu vzdy na server. Stranka sa berie najprv zo siete (nova verzia rozhrania hned).
const SHELL = "shell-v1";
const ASSETS = "assets-v1";
/** Najviac suborov /assets v pamati (stare verzie rozhrania sa postupne vymenia). */
const MAX_ASSETS = 300;
const STATIC = ["/favicon.svg", "/icon-192.png", "/icon-512.png", "/icon-maskable-512.png", "/apple-touch-icon.png", "/manifest.webmanifest"];

self.addEventListener("install", () => self.skipWaiting());

self.addEventListener("activate", (event) => {
  event.waitUntil(
    caches
      .keys()
      .then((keys) => Promise.all(keys.filter((key) => key !== SHELL && key !== ASSETS).map((key) => caches.delete(key))))
      .then(() => self.clients.claim()),
  );
});

async function trim(cache) {
  const keys = await cache.keys();
  for (const key of keys.slice(0, Math.max(0, keys.length - MAX_ASSETS))) await cache.delete(key);
}

/** Zo siete a ulozit; bez siete z pamate pod klucom key. */
async function networkFirst(request, key) {
  const cache = await caches.open(SHELL);
  try {
    const response = await fetch(request);
    if (response.ok) await cache.put(key, response.clone());
    return response;
  } catch (error) {
    const cached = await cache.match(key);
    if (cached) return cached;
    throw error;
  }
}

/** Subory /assets maju v mene obsah (hash) - z pamate, inak zo siete a ulozit. */
async function cacheFirst(request) {
  const cache = await caches.open(ASSETS);
  const cached = await cache.match(request);
  if (cached) return cached;
  const response = await fetch(request);
  if (response.ok) {
    await cache.put(request, response.clone());
    await trim(cache);
  }
  return response;
}

self.addEventListener("fetch", (event) => {
  const request = event.request;
  if (request.method !== "GET") return;
  const url = new URL(request.url);
  if (url.origin !== self.location.origin) return;
  // vsetky stranky su ta ista aplikacia (adresy za #) - jedna polozka
  if (request.mode === "navigate") event.respondWith(networkFirst(request, "/"));
  else if (url.pathname.startsWith("/assets/")) event.respondWith(cacheFirst(request));
  else if (STATIC.includes(url.pathname)) event.respondWith(networkFirst(request, url.pathname));
});
