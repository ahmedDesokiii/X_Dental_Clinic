/* ============================================================
 * 🔒 A1 — Antiforgery تلقائي لكل طلبات الـ AJAX
 * ------------------------------------------------------------
 * السيرفر بيطلب token مع كل POST/PUT/PATCH/DELETE
 * (AutoValidateAntiforgeryTokenAttribute في Program.cs).
 * الملف ده بيضيف الهيدر "RequestVerificationToken" أوتوماتيك على:
 *   - fetch()
 *   - jQuery $.ajax / $.post
 *   - XMLHttpRequest
 * بشرط إن الطلب لنفس الموقع (same-origin) ومش GET/HEAD.
 * يعني مفيش داعي نعدّل أي fetch موجود في الصفحات.
 * التوكن بيتقرا من <meta name="csrf-token"> في _Layout.
 * ============================================================ */
(function () {
    'use strict';

    var HEADER = 'RequestVerificationToken';
    var SAFE = /^(GET|HEAD|OPTIONS|TRACE)$/i;

    function getToken() {
        var m = document.querySelector('meta[name="csrf-token"]');
        if (m && m.content) return m.content;
        var i = document.querySelector('input[name="__RequestVerificationToken"]');
        return i ? i.value : '';
    }

    function isSameOrigin(url) {
        try { return new URL(url, window.location.href).origin === window.location.origin; }
        catch (e) { return true; }
    }

    window.getCsrfToken = getToken;

    // ── fetch ────────────────────────────────────────────────
    if (window.fetch) {
        var originalFetch = window.fetch;
        window.fetch = function (input, init) {
            try {
                var isRequest = (typeof Request !== 'undefined') && input instanceof Request;
                var method = ((init && init.method) || (isRequest ? input.method : 'GET') || 'GET');
                var url = isRequest ? input.url : String(input);

                if (!SAFE.test(method) && isSameOrigin(url)) {
                    var token = getToken();
                    if (token) {
                        init = init ? Object.assign({}, init) : {};
                        var headers = new Headers(init.headers || (isRequest ? input.headers : undefined));
                        if (!headers.has(HEADER)) headers.set(HEADER, token);
                        init.headers = headers;
                    }
                }
            } catch (e) { /* لو حصل أي خطأ نكمل بالطلب الأصلي */ }
            return originalFetch.call(this, input, init);
        };
    }

    // ── XMLHttpRequest ───────────────────────────────────────
    if (window.XMLHttpRequest) {
        var xo = XMLHttpRequest.prototype.open;
        var xs = XMLHttpRequest.prototype.send;
        var xh = XMLHttpRequest.prototype.setRequestHeader;

        XMLHttpRequest.prototype.open = function (method, url) {
            this.__afMethod = method;
            this.__afUrl = url;
            this.__afHasHeader = false;
            return xo.apply(this, arguments);
        };
        XMLHttpRequest.prototype.setRequestHeader = function (name, value) {
            if (String(name).toLowerCase() === HEADER.toLowerCase()) this.__afHasHeader = true;
            return xh.apply(this, arguments);
        };
        XMLHttpRequest.prototype.send = function () {
            try {
                if (!this.__afHasHeader && this.__afMethod && !SAFE.test(this.__afMethod) && isSameOrigin(this.__afUrl)) {
                    var token = getToken();
                    if (token) xh.call(this, HEADER, token);
                }
            } catch (e) { }
            return xs.apply(this, arguments);
        };
        // jQuery بيستخدم XMLHttpRequest، فهو متغطي هنا تلقائيًا.
    }
})();
