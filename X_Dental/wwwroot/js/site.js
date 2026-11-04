// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

// ============================================================
// DcBooking — حجز موعد مع تحذير لو المريض محجوز له في نفس اليوم
// الاستخدام: const r = await DcBooking.create(payload);
//   r === null  → المستخدم لغى بعد التحذير
//   r.success   → تم الحجز
// ============================================================
window.DcBooking = (function () {
    const esc = s => String(s ?? '').replace(/[&<>"']/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));

    async function post(payload) {
        const res = await fetch('/Appointments/Create', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(payload)
        });
        const text = await res.text();
        try {
            return JSON.parse(text);
        } catch {
            // السيرفر رجّع صفحة خطأ مش JSON — نوضح السبب بدل "مشكلة في الاتصال"
            console.error('Appointments/Create returned non-JSON', res.status, text.slice(0, 2000));
            throw new Error(`خطأ من السيرفر (HTTP ${res.status}) — التفاصيل في الـ Console`);
        }
    }

    function fmtDay(iso) {
        const d = new Date(iso);
        return d.toLocaleDateString('ar-EG', { weekday: 'long', day: 'numeric', month: 'long', year: 'numeric' });
    }

    async function confirmDuplicate(existing) {
        const first = existing[0] || {};
        const rows = existing.map(a => `
            <div class="dcb-row">
                <div class="dcb-row-main">
                    <b>${esc(a.reason || 'موعد')}</b>
                    <span>${esc(a.statusText)}</span>
                </div>
            </div>`).join('');

        const r = await Swal.fire({
            icon: 'warning',
            iconColor: '#f59e0b',
            title: 'موعد مكرر في نفس اليوم',
            html: `
                <div class="dcb-wrap" dir="rtl">
                    <div class="dcb-patient">
                        <i class="bi bi-person-circle"></i>
                        <div>
                            <b>${esc(first.patientName)}</b>
                            <span dir="ltr">${esc(first.patientMobile || '')}</span>
                        </div>
                    </div>
                    <div class="dcb-day"><i class="bi bi-calendar-event"></i> ${esc(fmtDay(first.appointmentDate))}</div>
                    <div class="dcb-note">المريض ده محجوز له بالفعل ${existing.length > 1 ? existing.length + ' مواعيد' : 'موعد'} في نفس اليوم:</div>
                    <div class="dcb-list">${rows}</div>
                </div>`,
            showCancelButton: true,
            confirmButtonText: '<i class="bi bi-calendar-plus"></i> احجز برضه',
            cancelButtonText: 'إلغاء',
            confirmButtonColor: '#0d9488',
            cancelButtonColor: '#94a3b8',
            reverseButtons: true,
            focusCancel: true,
            customClass: { popup: 'rounded-4 dcb-popup' }
        });
        return r.isConfirmed;
    }

    async function create(payload) {
        let data = await post(payload);
        if (!data.success && data.duplicate) {
            const ok = await confirmDuplicate(data.existing || []);
            if (!ok) return null;
            data = await post({ ...payload, AllowSameDay: true });
        }
        return data;
    }

    // ستايل التحذير (مرة واحدة)
    const css = document.createElement('style');
    css.textContent = `
        .dcb-popup .swal2-title{font-size:1.25rem;font-weight:800;color:#0b2e30}
        .dcb-wrap{text-align:right;display:flex;flex-direction:column;gap:10px}
        .dcb-patient{display:flex;align-items:center;gap:10px;background:#f0fdfa;border:1px solid #ccf3ec;border-radius:14px;padding:10px 14px}
        .dcb-patient i{font-size:1.8rem;color:#0d9488}
        .dcb-patient div{display:flex;flex-direction:column}
        .dcb-patient b{color:#0b2e30;font-size:.95rem}
        .dcb-patient span{color:#64748b;font-size:.78rem;text-align:right}
        .dcb-day{font-weight:800;color:#b45309;background:#fffbeb;border:1px solid #fde68a;border-radius:12px;padding:8px 12px;font-size:.85rem}
        .dcb-note{font-size:.85rem;color:#475569;font-weight:600}
        .dcb-list{display:flex;flex-direction:column;gap:6px;max-height:180px;overflow-y:auto}
        .dcb-row{background:#f8fafc;border:1px solid #e2e8f0;border-radius:10px;padding:8px 12px}
        .dcb-row-main{display:flex;justify-content:space-between;align-items:center;font-size:.82rem}
        .dcb-row-main span{font-size:.7rem;font-weight:800;background:#e0f2fe;color:#0369a1;padding:2px 8px;border-radius:8px}`;
    document.head.appendChild(css);

    return { create };
})();
