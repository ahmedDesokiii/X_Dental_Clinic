/* ============================================================
 * ⌨️ D2 — بحث سريع (Command Palette)
 * - يفتح بـ Ctrl+K / ⌘K أو "/" أو الضغط على خانة البحث العلوية أو زرار ➕
 * - بيدوّر على المرضى (اسم / كود / موبايل) + الإجراءات السريعة
 * - أسهم ↑↓ للتنقل، Enter للفتح، Esc للقفل
 * الإجراءات المتاحة بتيجي من الـ Layout حسب صلاحيات المستخدم (data-actions).
 * ============================================================ */
(function () {
    'use strict';

    var script = document.currentScript;
    var BASE = (script && script.getAttribute('data-base')) || '/';
    var ACTIONS = [];
    try { ACTIONS = JSON.parse((script && script.getAttribute('data-actions')) || '[]'); } catch (e) { }
    var CAN_SEARCH_PATIENTS = script && script.getAttribute('data-patients') === 'true';
    var WA_CC = (script && script.getAttribute('data-wa-cc')) || '20';

    var root, input, list, hint;
    var items = [], active = 0, timer = null, ctrl = null, lastQuery = '';

    function esc(s) {
        return String(s == null ? '' : s).replace(/[&<>"']/g, function (c) {
            return { '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c];
        });
    }
    function norm(s) {
        return String(s || '').toLowerCase()
            .replace(/[أإآ]/g, 'ا').replace(/ة/g, 'ه').replace(/ى/g, 'ي').trim();
    }
    function url(path) { return BASE + String(path).replace(/^\//, ''); }
    function waNumber(m) {
        var d = String(m || '').replace(/\D/g, '');
        if (!d) return '';
        if (d.indexOf('00') === 0) return d.slice(2);
        if (d.indexOf(WA_CC) === 0 && d.length > 10) return d;
        return WA_CC + d.replace(/^0/, '');
    }

    function build() {
        root = document.createElement('div');
        root.className = 'cp-root';
        root.setAttribute('dir', 'rtl');
        root.hidden = true;
        root.innerHTML =
            '<div class="cp-backdrop" data-close></div>' +
            '<div class="cp-panel" role="dialog" aria-modal="true" aria-label="بحث سريع">' +
            '  <div class="cp-search">' +
            '    <i class="bi bi-search"></i>' +
            '    <input type="text" autocomplete="off" spellcheck="false" placeholder="ابحث باسم المريض أو الكود أو الموبايل… أو اكتب إجراء" />' +
            '    <kbd>Esc</kbd>' +
            '  </div>' +
            '  <div class="cp-list" role="listbox"></div>' +
            '  <div class="cp-foot"><span><kbd>↑</kbd><kbd>↓</kbd> تنقّل</span><span><kbd>Enter</kbd> فتح</span><span class="cp-hint"></span></div>' +
            '</div>';
        document.body.appendChild(root);
        input = root.querySelector('input');
        list = root.querySelector('.cp-list');
        hint = root.querySelector('.cp-hint');

        root.addEventListener('click', function (e) {
            if (e.target.hasAttribute('data-close')) close();
            var it = e.target.closest('.cp-item');
            if (it) { e.preventDefault(); go(+it.getAttribute('data-i'), e.target.closest('[data-sub]')); }
        });
        root.addEventListener('mousemove', function (e) {
            var it = e.target.closest('.cp-item');
            if (it) setActive(+it.getAttribute('data-i'), false);
        });
        input.addEventListener('input', onInput);
        input.addEventListener('keydown', onKey);
    }

    function open(mode) {
        if (!root) build();
        root.hidden = false;
        document.documentElement.classList.add('cp-open');
        input.value = '';
        lastQuery = '';
        render([], mode === 'actions' ? 'إجراءات سريعة' : null);
        setTimeout(function () { input.focus(); }, 10);
    }
    function close() {
        if (!root) return;
        root.hidden = true;
        document.documentElement.classList.remove('cp-open');
        if (ctrl) ctrl.abort();
    }

    function actionMatches(q) {
        if (!q) return ACTIONS.slice();
        var nq = norm(q);
        return ACTIONS.filter(function (a) { return norm(a.title + ' ' + (a.keywords || '')).indexOf(nq) !== -1; });
    }

    function render(patients, label) {
        var q = input.value.trim();
        var acts = actionMatches(q);
        items = [];
        var html = '';

        if (patients.length) {
            html += '<div class="cp-group">المرضى</div>';
            patients.forEach(function (p) {
                var i = items.length;
                items.push({ type: 'patient', p: p });
                var wa = waNumber(p.phone);
                html +=
                    '<a class="cp-item" data-i="' + i + '" href="' + url('Patients/Details/' + p.id) + '">' +
                    '  <span class="cp-avatar">' + esc((p.fullNameAr || '?').trim().charAt(0)) + '</span>' +
                    '  <span class="cp-main"><b>' + esc(p.fullNameAr) + '</b>' +
                    '    <small>' + esc(p.patientCode || '') + (p.phone ? ' · <span dir="ltr">' + esc(p.phone) + '</span>' : '') + '</small></span>' +
                    '  <span class="cp-subs">' +
                    (p.phone ? '<span class="cp-sub" data-sub="tel" title="اتصال"><i class="bi bi-telephone"></i></span>' : '') +
                    (wa ? '<span class="cp-sub cp-wa" data-sub="wa" title="واتساب"><i class="bi bi-whatsapp"></i></span>' : '') +
                    '  </span>' +
                    '</a>';
            });
        }

        if (acts.length) {
            html += '<div class="cp-group">' + (label || 'إجراءات') + '</div>';
            acts.forEach(function (a) {
                var i = items.length;
                items.push({ type: 'action', a: a });
                html +=
                    '<a class="cp-item" data-i="' + i + '" href="' + esc(url(a.url)) + '">' +
                    '  <span class="cp-icon"><i class="bi ' + esc(a.icon) + '"></i></span>' +
                    '  <span class="cp-main"><b>' + esc(a.title) + '</b>' + (a.desc ? '<small>' + esc(a.desc) + '</small>' : '') + '</span>' +
                    '</a>';
            });
        }

        if (!items.length) {
            html = q.length >= 2
                ? '<div class="cp-empty"><i class="bi bi-emoji-neutral"></i>مفيش نتايج لـ «' + esc(q) + '»</div>'
                : '<div class="cp-empty"><i class="bi bi-keyboard"></i>اكتب حرفين على الأقل للبحث عن مريض</div>';
        }

        list.innerHTML = html;
        hint.textContent = CAN_SEARCH_PATIENTS ? '' : '';
        setActive(0, true);
    }

    function setActive(i, scroll) {
        active = Math.max(0, Math.min(i, items.length - 1));
        var nodes = list.querySelectorAll('.cp-item');
        nodes.forEach(function (n, k) { n.classList.toggle('active', k === active); });
        if (scroll && nodes[active]) nodes[active].scrollIntoView({ block: 'nearest' });
    }

    function go(i, sub) {
        var it = items[i];
        if (!it) return;
        if (it.type === 'patient') {
            if (sub && sub.getAttribute('data-sub') === 'tel') { location.href = 'tel:' + it.p.phone; return; }
            if (sub && sub.getAttribute('data-sub') === 'wa') { window.open('https://wa.me/' + waNumber(it.p.phone), '_blank', 'noopener'); return; }
            location.href = url('Patients/Details/' + it.p.id);
        } else {
            location.href = url(it.a.url);
        }
    }

    function onKey(e) {
        if (e.key === 'ArrowDown') { e.preventDefault(); setActive(active + 1, true); }
        else if (e.key === 'ArrowUp') { e.preventDefault(); setActive(active - 1, true); }
        else if (e.key === 'Enter') { e.preventDefault(); go(active); }
        else if (e.key === 'Escape') { e.preventDefault(); close(); }
    }

    function onInput() {
        var q = input.value.trim();
        clearTimeout(timer);
        if (!CAN_SEARCH_PATIENTS || q.length < 2) { render([]); return; }
        render([]);   // الإجراءات فورًا
        timer = setTimeout(function () { search(q); }, 220);
    }

    function search(q) {
        if (q === lastQuery) return;
        lastQuery = q;
        if (ctrl) ctrl.abort();
        ctrl = typeof AbortController !== 'undefined' ? new AbortController() : null;
        list.classList.add('cp-loading');
        fetch(url('Patients/Search') + '?term=' + encodeURIComponent(q), {
            headers: { 'X-Requested-With': 'XMLHttpRequest', 'Accept': 'application/json' },
            signal: ctrl ? ctrl.signal : undefined
        })
            .then(function (r) { return r.ok ? r.json() : []; })
            .then(function (data) {
                if (input.value.trim() !== q) return;
                var arr = Array.isArray(data) ? data : [];
                render(arr.map(function (p) {
                    return {
                        id: p.id ?? p.Id, fullNameAr: p.fullNameAr ?? p.FullNameAr,
                        patientCode: p.patientCode ?? p.PatientCode, phone: p.phone ?? p.Phone
                    };
                }).slice(0, 8));
            })
            .catch(function () { })
            .finally(function () { list.classList.remove('cp-loading'); });
    }

    // ── فتح ─────────────────────────────────────────────────
    document.addEventListener('keydown', function (e) {
        var typing = /^(INPUT|TEXTAREA|SELECT)$/.test((e.target.tagName || '')) || e.target.isContentEditable;
        if ((e.ctrlKey || e.metaKey) && (e.key === 'k' || e.key === 'K')) { e.preventDefault(); open(); }
        else if (e.key === '/' && !typing) { e.preventDefault(); open(); }
    });

    document.addEventListener('DOMContentLoaded', function () {
        var top = document.querySelector('.topbar-search');
        if (top) {
            var ti = top.querySelector('input');
            if (ti) { ti.readOnly = true; ti.placeholder = 'بحث سريع… (Ctrl+K)'; }
            top.style.cursor = 'pointer';
            top.addEventListener('click', function () { open(); });
        }
        var mobileBtn = document.getElementById('cpMobileBtn');
        if (mobileBtn) mobileBtn.addEventListener('click', function () { open(); });
    });

    window.openCommandPalette = open;
    window.showQuickActions = function () { open('actions'); };   // زرار ➕
})();
