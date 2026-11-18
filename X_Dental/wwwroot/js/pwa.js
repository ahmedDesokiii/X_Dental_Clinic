/* ============================================================
 * 📱 C1 — PWA: تسجيل الـ Service Worker + زر التثبيت + إشعار التحديث
 * ============================================================ */
(function () {
    'use strict';

    var script = document.currentScript;
    var BASE = (script && script.getAttribute('data-base')) || '/';

    var isStandalone = function () {
        return window.matchMedia('(display-mode: standalone)').matches || window.navigator.standalone === true;
    };
    var isIOS = /iphone|ipad|ipod/i.test(navigator.userAgent) ||
        (navigator.platform === 'MacIntel' && navigator.maxTouchPoints > 1);
    var hasSwal = function () { return typeof window.Swal !== 'undefined'; };
    var installBtn = function () { return document.getElementById('pwaInstallBtn'); };

    if (isStandalone()) document.documentElement.classList.add('pwa-standalone');

    // ── لون شريط الحالة يتبع الوضع الليلي ─────────────────────
    function syncThemeColor() {
        var meta = document.querySelector('meta[name="theme-color"]');
        if (!meta) return;
        meta.setAttribute('content',
            document.documentElement.getAttribute('data-theme') === 'dark' ? '#0f172a' : '#0d7377');
    }
    syncThemeColor();
    new MutationObserver(syncThemeColor)
        .observe(document.documentElement, { attributes: true, attributeFilter: ['data-theme'] });

    // ── 1) Service Worker ─────────────────────────────────────
    if ('serviceWorker' in navigator) {
        if (!window.isSecureContext) {
            console.warn('[PWA] الموقع مش HTTPS — التثبيت كتطبيق مش متاح على الموبايل.');
        } else {
            window.addEventListener('load', function () {
                navigator.serviceWorker.register(BASE + 'sw.js', { scope: BASE }).then(function (reg) {
                    if (reg.waiting) promptUpdate(reg.waiting);
                    reg.addEventListener('updatefound', function () {
                        var nw = reg.installing;
                        if (!nw) return;
                        nw.addEventListener('statechange', function () {
                            if (nw.state === 'installed' && navigator.serviceWorker.controller) promptUpdate(nw);
                        });
                    });
                    // التطبيق ممكن يفضل مفتوح طول اليوم في العيادة → نشيّك على تحديث كل ساعة
                    setInterval(function () { reg.update().catch(function () { }); }, 60 * 60 * 1000);
                }).catch(function (e) { console.error('[PWA] SW registration failed', e); });
            });

            var refreshing = false;
            navigator.serviceWorker.addEventListener('controllerchange', function () {
                if (refreshing) return;
                refreshing = true;
                location.reload();
            });
        }
    }

    function hasPendingSessionWork() {
        var count = function (x) {
            if (Array.isArray(x)) return x.length;
            if (x && typeof x === 'object') return Object.keys(x).length;
            return 0;
        };
        return count(window.pendingToothRecords) + count(window.pendingPayments) > 0;
    }

    function promptUpdate(worker) {
        var apply = function () { worker.postMessage({ type: 'SKIP_WAITING' }); };
        // جلسة فيها بيانات لسه ما اتحفظتش → ما نقاطعش. التحديث هيتطبق مع أول فتح جديد.
        if (hasPendingSessionWork()) return;

        if (hasSwal()) {
            Swal.fire({
                toast: true, position: 'bottom', icon: 'info',
                title: 'يتوفر تحديث جديد للتطبيق',
                showConfirmButton: true, confirmButtonText: 'تحديث الآن',
                showCancelButton: true, cancelButtonText: 'لاحقًا'
            }).then(function (r) { if (r.isConfirmed) apply(); });
        } else if (confirm('يتوفر تحديث جديد للتطبيق. تحديث الآن؟')) {
            apply();
        }
    }

    // ── 2) زر التثبيت (Android / Chrome / Edge) ────────────────
    var deferredPrompt = null;

    window.addEventListener('beforeinstallprompt', function (e) {
        e.preventDefault();
        deferredPrompt = e;
        var b = installBtn();
        if (b) b.hidden = false;
    });

    document.addEventListener('click', function (e) {
        if (!e.target.closest || !e.target.closest('#pwaInstallBtn')) return;
        if (deferredPrompt) {
            deferredPrompt.prompt();
            deferredPrompt.userChoice.then(function (choice) {
                deferredPrompt = null;
                if (choice.outcome === 'accepted') { var b = installBtn(); if (b) b.hidden = true; }
            });
        } else if (isIOS) {
            showIOSHint(true);
        }
    });

    window.addEventListener('appinstalled', function () {
        var b = installBtn(); if (b) b.hidden = true;
        if (hasSwal()) Swal.fire({ toast: true, position: 'top', icon: 'success', title: 'تم تثبيت التطبيق', timer: 2500, showConfirmButton: false });
    });

    // ── 3) iPhone / iPad: مفيش prompt → إرشاد مرة واحدة ──────
    function showIOSHint(force) {
        var KEY = 'pwa-ios-hint-shown';
        try {
            if (!force && localStorage.getItem(KEY)) return;
            localStorage.setItem(KEY, '1');
        } catch (e) { }
        var html = 'من Safari اضغط زر <b>المشاركة</b> <i class="bi bi-box-arrow-up"></i> ثم اختر <b>«إضافة إلى الشاشة الرئيسية»</b>.';
        if (hasSwal()) Swal.fire({ icon: 'info', title: 'تثبيت التطبيق على iPhone / iPad', html: html, confirmButtonText: 'تمام' });
        else alert('Safari ← مشاركة ← إضافة إلى الشاشة الرئيسية');
    }

    window.addEventListener('load', function () {
        if (isIOS && !isStandalone() && window.isSecureContext) {
            var b = installBtn(); if (b) b.hidden = false;
            setTimeout(function () { showIOSHint(false); }, 4000);
        }
    });
})();
