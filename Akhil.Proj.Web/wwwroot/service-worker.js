const CACHE_NAME = 'cmc-cache-v1';
const assetsToCache = [
    '/',
    '/app.css',
    '/favicon.png',
    '/manifest.json',
    '/icon-192.png',
    '/icon-512.png',
    '/_framework/blazor.web.js',
    '/lib/bootstrap/dist/css/bootstrap.min.css'
];

// Install Event
self.addEventListener('install', event => {
    self.skipWaiting();
    event.waitUntil(
        caches.open(CACHE_NAME)
            .then(cache => {
                console.log('[Service Worker] Caching static assets');
                return cache.addAll(assetsToCache);
            })
    );
});

// Activate Event
self.addEventListener('activate', event => {
    event.waitUntil(
        caches.keys().then(cacheNames => {
            return Promise.all(
                cacheNames.map(cache => {
                    if (cache !== CACHE_NAME) {
                        console.log('[Service Worker] Clearing old cache');
                        return caches.delete(cache);
                    }
                })
            );
        }).then(() => self.clients.claim())
    );
});

// Fetch Event
self.addEventListener('fetch', event => {
    // Only cache GET requests
    if (event.request.method !== 'GET') return;

    const url = new URL(event.request.url);

    // Skip caching for Blazor hot reload / WebSocket connections
    if (url.pathname.includes('/_blazor') || url.pathname.includes('/_framework/aspnetcore-browser-refresh.js')) {
        return;
    }

    const isStaticAsset = assetsToCache.some(path => url.pathname === path) || 
                          url.pathname.startsWith('/_framework/') ||
                          url.pathname.startsWith('/lib/') ||
                          url.pathname.startsWith('/fonts/');

    if (isStaticAsset) {
        event.respondWith(
            caches.match(event.request)
                .then(cachedResponse => {
                    if (cachedResponse) {
                        // Return cached asset, fetch in background to update cache (stale-while-revalidate)
                        fetch(event.request).then(networkResponse => {
                            if (networkResponse.status === 200) {
                                caches.open(CACHE_NAME).then(cache => cache.put(event.request, networkResponse));
                            }
                        }).catch(() => { /* Ignore offline fetch errors */ });
                        return cachedResponse;
                    }
                    return fetch(event.request).then(networkResponse => {
                        if (networkResponse.status === 200) {
                            const responseClone = networkResponse.clone();
                            caches.open(CACHE_NAME).then(cache => cache.put(event.request, responseClone));
                        }
                        return networkResponse;
                    });
                })
        );
    }
});
