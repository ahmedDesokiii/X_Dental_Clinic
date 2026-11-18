/* ============================================================
 * 📱 C1 — Service Worker (PWA)
 * - الصفحات: من السيرفر دايمًا. بيانات المرضى ما بتتخزنش على الجهاز.
 *   لو مفيش اتصال → offline.html
 * - الملفات الثابتة (css / js / lib / fonts / icons / images): Stale-While-Revalidate
 * - أي طلب غير GET، أو /uploads (الأشعة والمستندات)، أو أي API: ما بيتلمسش.
 * - شغال حتى لو البرنامج منشور تحت مسار فرعي (مثلاً /dental/).
 * ⚠️ غيّري VERSION مع كل نشر عشان الأجهزة تاخد التحديث.
 * ============================================================ */
const VERSION = 'v1.0.0';
const CACHE = `dental-static-${VERSION}`;
const BASE = new URL(self.registration.scope).pathname;           // "/" أو "/dental/"
const OFFLINE_URL = BASE + 'offline.html';

const PRECACHE = [
    OFFLINE_URL,
    BASE + 'icons/icon-192.png',
    BASE + 'icons/icon-512.png',
    BASE + 'manifest.json'
];

const STATIC_DIRS = ['css/', 'js/', 'lib/', 'fonts/', 'icons/', 'images/', 'sweetalert2/']
    .map(d => BASE + d);

self.addEventListener('install', (event) => {
    event.waitUntil(caches.open(CACHE).then((c) => c.addAll(PRECACHE)));
    // مفيش skipWaiting تلقائي: المستخدم يوافق من رسالة التحديث،
    // عشان ما يتقطعش عليه شغل جلسة مفتوحة.
});

self.addEventListener('activate', (event) => {
    event.waitUntil((async () => {
        const keys = await caches.keys();
        await Promise.all(keys
            .filter((k) => k.startsWith('dental-static-') && k !== CACHE)
            .map((k) => caches.delete(k)));
        if (self.registration.navigationPreload) {
            await self.registration.navigationPreload.enable();
        }
        await self.clients.claim();
    })());
});

self.addEventListener('message', (event) => {
    if (event.data && event.data.type === 'SKIP_WAITING') self.skipWaiting();
});

self.addEventListener('fetch', (event) => {
    const req = event.request;
    if (req.method !== 'GET') return;

    const url = new URL(req.url);
    if (url.origin !== self.location.origin) return;

    // 1) تنقّل بين الصفحات
    if (req.mode === 'navigate') {
        event.respondWith((async () => {
            try {
                const preload = await event.preloadResponse;
                if (preload) return preload;
                return await fetch(req);
            } catch {
                const cache = await caches.open(CACHE);
                return (await cache.match(OFFLINE_URL)) || new Response('Offline', { status: 503 });
            }
        })());
        return;
    }

    // 2) ملفات ثابتة فقط
    if (STATIC_DIRS.some((d) => url.pathname.startsWith(d))) {
        event.respondWith((async () => {
            const cache = await caches.open(CACHE);
            const cached = await cache.match(req);
            const network = fetch(req).then((res) => {
                if (res && res.ok && res.type === 'basic') cache.put(req, res.clone());
                return res;
            }).catch(() => cached);
            return cached || network;
        })());
    }
    // 3) أي حاجة تانية (API، /uploads، ...) → المتصفح يتعامل معاها عادي
});
