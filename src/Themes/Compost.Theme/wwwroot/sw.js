/**
 * Compost PWA Service Worker
 * Provides offline functionality and caching strategies
 */

const CACHE_NAME = 'compost-v1.0.0';
const STATIC_CACHE = 'compost-static-v1.0.0';
const DYNAMIC_CACHE = 'compost-dynamic-v1.0.0';

// Files to cache for offline functionality
const STATIC_ASSETS = [
    '/',
    '/Compost.Theme/css/compost.css',
    '/Compost.Theme/js/compost.js',
    'https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700;800&family=JetBrains+Mono:wght@400;500;600&display=swap',
    'https://cdnjs.cloudflare.com/ajax/libs/font-awesome/6.4.0/css/all.min.css',
    'https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/js/bootstrap.bundle.min.js'
];

// Install event - cache static assets
self.addEventListener('install', (event) => {
    console.log('Service Worker: Installing...');
    
    event.waitUntil(
        caches.open(STATIC_CACHE)
            .then((cache) => {
                console.log('Service Worker: Caching static assets');
                return cache.addAll(STATIC_ASSETS);
            })
            .then(() => {
                console.log('Service Worker: Static assets cached');
                return self.skipWaiting();
            })
            .catch((error) => {
                console.error('Service Worker: Failed to cache static assets', error);
            })
    );
});

// Activate event - clean up old caches
self.addEventListener('activate', (event) => {
    console.log('Service Worker: Activating...');
    
    event.waitUntil(
        caches.keys()
            .then((cacheNames) => {
                return Promise.all(
                    cacheNames.map((cacheName) => {
                        if (cacheName !== STATIC_CACHE && cacheName !== DYNAMIC_CACHE) {
                            console.log('Service Worker: Deleting old cache', cacheName);
                            return caches.delete(cacheName);
                        }
                    })
                );
            })
            .then(() => {
                console.log('Service Worker: Old caches cleaned up');
                return self.clients.claim();
            })
    );
});

// Fetch event - implement caching strategies
self.addEventListener('fetch', (event) => {
    const { request } = event;
    const url = new URL(request.url);
    
    // Skip non-GET requests
    if (request.method !== 'GET') {
        return;
    }
    
    // Skip external API requests
    if (url.origin !== location.origin && !url.hostname.includes('fonts.googleapis.com') && !url.hostname.includes('cdnjs.cloudflare.com')) {
        return;
    }
    
    event.respondWith(
        caches.match(request)
            .then((response) => {
                // Cache hit - return cached response
                if (response) {
                    console.log('Service Worker: Serving from cache', request.url);
                    return response;
                }
                
                // Cache miss - fetch from network
                console.log('Service Worker: Fetching from network', request.url);
                return fetch(request)
                    .then((networkResponse) => {
                        // Don't cache non-successful responses
                        if (!networkResponse || networkResponse.status !== 200 || networkResponse.type !== 'basic') {
                            return networkResponse;
                        }
                        
                        // Clone the response since it can only be consumed once
                        const responseClone = networkResponse.clone();
                        
                        // Cache the response for future use
                        caches.open(DYNAMIC_CACHE)
                            .then((cache) => {
                                console.log('Service Worker: Caching dynamic response', request.url);
                                cache.put(request, responseClone);
                            })
                            .catch((error) => {
                                console.error('Service Worker: Failed to cache dynamic response', error);
                            });
                        
                        return networkResponse;
                    })
                    .catch((error) => {
                        console.error('Service Worker: Network request failed', error);
                        
                        // Try to serve from cache as fallback
                        return caches.match(request)
                            .then((fallbackResponse) => {
                                if (fallbackResponse) {
                                    console.log('Service Worker: Serving fallback from cache', request.url);
                                    return fallbackResponse;
                                }
                                
                                // Return offline page for navigation requests
                                if (request.mode === 'navigate') {
                                    return caches.match('/Compost.Theme/offline.html');
                                }
                                
                                // Return error for other requests
                                return new Response('Offline', {
                                    status: 503,
                                    statusText: 'Service Unavailable'
                                });
                            });
                    });
            })
    );
});

// Background sync for offline actions
self.addEventListener('sync', (event) => {
    console.log('Service Worker: Background sync triggered', event.tag);
    
    if (event.tag === 'background-sync') {
        event.waitUntil(doBackgroundSync());
    }
});

async function doBackgroundSync() {
    try {
        // Get all pending actions from IndexedDB
        const pendingActions = await getPendingActions();
        
        // Process each pending action
        for (const action of pendingActions) {
            try {
                await fetch(action.url, action.options);
                await removePendingAction(action.id);
                console.log('Service Worker: Background sync completed for action', action.id);
            } catch (error) {
                console.error('Service Worker: Background sync failed for action', action.id, error);
            }
        }
    } catch (error) {
        console.error('Service Worker: Background sync failed', error);
    }
}

// Push notifications
self.addEventListener('push', (event) => {
    console.log('Service Worker: Push notification received');
    
    const options = {
        body: event.data ? event.data.text() : 'New notification from Compost',
        icon: '/Compost.Theme/images/icon-192x192.png',
        badge: '/Compost.Theme/images/badge-72x72.png',
        vibrate: [100, 50, 100],
        data: {
            dateOfArrival: Date.now(),
            primaryKey: 1
        },
        actions: [
            {
                action: 'explore',
                title: 'Explore',
                icon: '/Compost.Theme/images/checkmark.png'
            },
            {
                action: 'close',
                title: 'Close',
                icon: '/Compost.Theme/images/xmark.png'
            }
        ]
    };
    
    event.waitUntil(
        self.registration.showNotification('Compost', options)
    );
});

// Notification click handling
self.addEventListener('notificationclick', (event) => {
    console.log('Service Worker: Notification click received');
    
    event.notification.close();
    
    if (event.action === 'explore') {
        event.waitUntil(
            clients.openWindow('/')
        );
    } else if (event.action === 'close') {
        // Just close the notification
    } else {
        // Default action - open the app
        event.waitUntil(
            clients.openWindow('/')
        );
    }
});

// Message handling from main thread
self.addEventListener('message', (event) => {
    console.log('Service Worker: Message received', event.data);
    
    if (event.data && event.data.type === 'SKIP_WAITING') {
        self.skipWaiting();
    }
    
    if (event.data && event.data.type === 'CACHE_UPDATE') {
        updateCache();
    }
});

// Cache update function
async function updateCache() {
    try {
        const cache = await caches.open(STATIC_CACHE);
        await cache.addAll(STATIC_ASSETS);
        console.log('Service Worker: Cache updated successfully');
    } catch (error) {
        console.error('Service Worker: Cache update failed', error);
    }
}

// IndexedDB helpers for offline actions
async function getPendingActions() {
    return new Promise((resolve, reject) => {
        const request = indexedDB.open('CompostOfflineDB', 1);
        
        request.onerror = () => reject(request.error);
        request.onsuccess = () => {
            const db = request.result;
            const transaction = db.transaction(['pendingActions'], 'readonly');
            const store = transaction.objectStore('pendingActions');
            const getAllRequest = store.getAll();
            
            getAllRequest.onerror = () => reject(getAllRequest.error);
            getAllRequest.onsuccess = () => resolve(getAllRequest.result);
        };
        
        request.onupgradeneeded = () => {
            const db = request.result;
            if (!db.objectStoreNames.contains('pendingActions')) {
                db.createObjectStore('pendingActions', { keyPath: 'id' });
            }
        };
    });
}

async function removePendingAction(id) {
    return new Promise((resolve, reject) => {
        const request = indexedDB.open('CompostOfflineDB', 1);
        
        request.onerror = () => reject(request.error);
        request.onsuccess = () => {
            const db = request.result;
            const transaction = db.transaction(['pendingActions'], 'readwrite');
            const store = transaction.objectStore('pendingActions');
            const deleteRequest = store.delete(id);
            
            deleteRequest.onerror = () => reject(deleteRequest.error);
            deleteRequest.onsuccess = () => resolve();
        };
    });
}

// Network status monitoring
self.addEventListener('online', () => {
    console.log('Service Worker: Client is online');
    // Trigger background sync when coming back online
    self.registration.sync.register('background-sync');
});

self.addEventListener('offline', () => {
    console.log('Service Worker: Client is offline');
});
