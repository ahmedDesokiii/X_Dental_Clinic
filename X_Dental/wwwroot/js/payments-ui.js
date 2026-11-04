// ============================================================
// 📁 wwwroot/js/payments-ui.js  (ملف جديد — المرحلة 5)
// واجهة المدفوعات المشتركة بين الـ Details وكشف الحساب:
//   • receiptRows()  : الدفعة كإيصال واحد (سطر واحد بالإجمالي) + تفاصيل التوزيع على الزيارات
//   • allocate()     : معاينة توزيع الدفعة قبل الحفظ — نفس قواعد السيرفر بالظبط (PaymentService.AllocateAsync)
//   • renderPreview(): عرض المعاينة جوه المودال
// ============================================================
window.DcPay = (function () {
    const T = {
        ar: {
            visits: n => `${n} زيارات`, visit: 'الزيارة', arrears: 'سداد متأخرات', main: 'الزيارة المختارة',
            discount: 'خصم', refund: 'استرداد', voidBtn: 'إلغاء', split: 'موزعة على', credit: 'رصيد دائن للمريض',
            pvTitle: 'توزيع الدفعة', pvTarget: 'على الزيارة المختارة', pvOld: 'سداد زيارة', pvCredit: 'زيادة عن كل المطلوب — هتتسجل رصيد دائن',
            pvNone: 'ادخل المبلغ لمعاينة التوزيع', pvDiscOver: 'الخصم أكبر من متبقي الزيارة',
            pvAfter: 'متبقي المريض بعد الدفعة', empty: 'لا توجد دفعات مسجلة', locale: 'ar-EG'
        },
        en: {
            visits: n => `${n} visits`, visit: 'Visit', arrears: 'Arrears', main: 'Selected visit',
            discount: 'Discount', refund: 'Refund', voidBtn: 'Void', split: 'Split across', credit: 'Patient credit',
            pvTitle: 'Payment allocation', pvTarget: 'To the selected visit', pvOld: 'Settles visit', pvCredit: 'Above everything due — recorded as patient credit',
            pvNone: 'Enter an amount to preview the allocation', pvDiscOver: 'Discount is larger than the visit remaining',
            pvAfter: 'Patient remaining after payment', empty: 'No payments recorded yet', locale: 'en-GB'
        }
    };

    const esc = s => String(s ?? '').replace(/[&<>"']/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));
    const num = v => Number(v || 0);
    const fmtMoney = (v, lang) => num(v).toLocaleString(lang === 'ar' ? 'ar-EG' : 'en-US', { minimumFractionDigits: 0, maximumFractionDigits: 2 });
    const fmtDate = (d, lang) => new Date(d).toLocaleDateString(T[lang].locale);
    const fmtTime = (d, lang) => new Date(d).toLocaleTimeString(T[lang].locale, { hour: 'numeric', minute: '2-digit' });

    // ── الدفعات كإيصالات ──
    // opts: { lang, voidFn: 'voidTabPayment', methodClass: fn(text) → class }
    function receiptRows(receipts, opts) {
        const lang = opts.lang || 'en', t = T[lang];
        return receipts.map((r, i) => {
            const parts = r.parts || [];
            const split = parts.length > 1;
            const visitCell = split
                ? `<button type="button" class="dcp-split" onclick="DcPay.toggle(this)" title="${esc(t.split)} ${parts.length}">
                       <i class="bi bi-diagram-3"></i> ${esc(t.visits(parts.length))} <i class="bi bi-chevron-down dcp-chev"></i>
                   </button>`
                : (parts[0] ? fmtDate(parts[0].visitDate, lang) : '—');
            const amountCls = r.amount < 0 ? 'neg' : '';
            const disc = num(r.discount) > 0 ? `<span class="dcp-disc"><i class="bi bi-percent"></i> ${esc(t.discount)} ${fmtMoney(r.discount, lang)}</span>` : '';
            const refund = r.amount < 0 ? `<span class="dcp-disc neg">${esc(t.refund)}</span>` : '';
            const mCls = opts.methodClass ? opts.methodClass(r.methodText) : '';
            const voidBtn = opts.voidFn
                ? `<button class="${opts.voidClass || 'btn-void'}" onclick="${opts.voidFn}(${r.paymentId}, ${parts.length})"><i class="bi bi-x-circle"></i> ${esc(t.voidBtn)}</button>`
                : '';

            const main = `
                <tr class="dcp-row" style="animation-delay:${Math.min(i * 0.04, 0.3)}s">
                    <td><span class="dcp-when">${fmtDate(r.paymentDate, lang)}<small>${fmtTime(r.paymentDate, lang)}</small></span></td>
                    <td>${visitCell}</td>
                    <td><b class="dcp-amt ${amountCls}">${fmtMoney(r.amount, lang)}</b>${disc}${refund}</td>
                    <td><span class="${opts.methodBadge || 'pmt-method-badge'} ${mCls}">${esc(r.methodText)}</span></td>
                    <td style="direction:ltr;">${esc(r.receiptNo || '—')}</td>
                    <td class="dcp-desc">${esc(r.description || '—')}</td>
                    <td>${voidBtn}</td>
                </tr>`;

            if (!split) return main;

            const sub = `
                <tr class="dcp-parts" hidden>
                    <td colspan="7">
                        <div class="dcp-parts-list">
                            ${parts.map(p => `
                                <div class="dcp-part ${p.isArrears ? 'arrears' : 'main'}">
                                    <span class="dcp-part-dot"></span>
                                    <span class="dcp-part-visit"><i class="bi bi-calendar3"></i> ${esc(t.visit)} ${fmtDate(p.visitDate, lang)}</span>
                                    <span class="dcp-part-tag">${esc(p.isArrears ? t.arrears : t.main)}</span>
                                    <b class="dcp-part-amt">${fmtMoney(p.amount, lang)}</b>
                                </div>`).join('')}
                        </div>
                    </td>
                </tr>`;
            return main + sub;
        }).join('');
    }

    function toggle(btn) {
        const row = btn.closest('tr');
        const sub = row && row.nextElementSibling;
        if (!sub || !sub.classList.contains('dcp-parts')) return;
        sub.hidden = !sub.hidden;
        btn.classList.toggle('open', !sub.hidden);
    }

    // ── معاينة التوزيع — نفس قواعد السيرفر ──
    // visits: [{visitId, visitDate, category, remaining}]
    function allocate(visits, targetId, cash, discount) {
        const target = visits.find(v => v.visitId === targetId);
        if (!target) return null;
        cash = Math.max(0, num(cash));
        discount = Math.max(0, num(discount));

        const targetDue = Math.max(0, num(target.remaining) - discount);
        const toTarget = Math.min(cash, targetDue);
        let left = cash - toTarget;

        const older = visits
            .filter(v => v.visitId !== targetId && v.category === target.category && num(v.remaining) > 0)
            .sort((a, b) => new Date(a.visitDate) - new Date(b.visitDate) || a.visitId - b.visitId);

        const arrears = [];
        for (const o of older) {
            if (left <= 0) break;
            const part = Math.min(left, num(o.remaining));
            arrears.push({ visitId: o.visitId, visitDate: o.visitDate, amount: part });
            left -= part;
        }

        const totalDueBefore = visits.filter(v => v.category === target.category).reduce((s, v) => s + Math.max(0, num(v.remaining)), 0);
        return {
            target, toTarget, arrears, credit: left,
            discountOver: discount > Math.max(0, num(target.remaining)),
            remainingAfter: Math.max(0, totalDueBefore - discount - (cash - left))
        };
    }

    function renderPreview(el, visits, targetId, cash, discount, lang) {
        const t = T[lang || 'en'];
        if (!el) return;
        if (!(num(cash) > 0 || num(discount) > 0)) {
            el.innerHTML = `<div class="dcp-pv-empty"><i class="bi bi-calculator"></i> ${esc(t.pvNone)}</div>`;
            return;
        }
        const a = allocate(visits, targetId, cash, discount);
        if (!a) { el.innerHTML = ''; return; }
        if (a.discountOver) {
            el.innerHTML = `<div class="dcp-pv-warn"><i class="bi bi-exclamation-triangle"></i> ${esc(t.pvDiscOver)} (${fmtMoney(a.target.remaining, lang)})</div>`;
            return;
        }

        const lines = [];
        if (a.toTarget > 0 || num(discount) > 0) {
            lines.push(`<div class="dcp-pv-line main"><span><i class="bi bi-bullseye"></i> ${esc(t.pvTarget)} (${fmtDate(a.target.visitDate, lang)})</span>
                <b>${fmtMoney(a.toTarget, lang)}${num(discount) > 0 ? ` <small>+ ${esc(t.discount)} ${fmtMoney(discount, lang)}</small>` : ''}</b></div>`);
        }
        a.arrears.forEach(p => lines.push(`<div class="dcp-pv-line arrears"><span><i class="bi bi-arrow-return-left"></i> ${esc(t.pvOld)} ${fmtDate(p.visitDate, lang)}</span><b>${fmtMoney(p.amount, lang)}</b></div>`));
        if (a.credit > 0) lines.push(`<div class="dcp-pv-line credit"><span><i class="bi bi-piggy-bank"></i> ${esc(t.pvCredit)}</span><b>${fmtMoney(a.credit, lang)}</b></div>`);

        el.innerHTML = `
            <div class="dcp-pv">
                <div class="dcp-pv-head"><i class="bi bi-diagram-3"></i> ${esc(t.pvTitle)}</div>
                ${lines.join('')}
                <div class="dcp-pv-foot"><span>${esc(t.pvAfter)}</span><b>${fmtMoney(a.remainingAfter, lang)}</b></div>
            </div>`;
    }

    // ── ستايل مشترك (مرة واحدة) ──
    const css = document.createElement('style');
    css.textContent = `
        .dcp-row { animation: dcpFade .3s ease both; }
        @keyframes dcpFade { from { opacity: 0; transform: translateY(4px); } to { opacity: 1; transform: none; } }
        .dcp-when { display: flex; flex-direction: column; line-height: 1.3; }
        .dcp-when small { font-size: .7rem; color: #94a3b8; font-weight: 700; }
        .dcp-amt { font-weight: 800; color: #047857; }
        .dcp-amt.neg { color: #dc2626; }
        .dcp-disc { display: inline-flex; align-items: center; gap: 3px; margin-inline-start: 6px; font-size: .68rem; font-weight: 800;
            color: #b45309; background: #fffbeb; border: 1px solid #fde68a; padding: 1px 7px; border-radius: 8px; white-space: nowrap; }
        .dcp-disc.neg { color: #dc2626; background: #fef2f2; border-color: #fecaca; }
        .dcp-desc { max-width: 260px; }
        .dcp-split { border: 1px solid #c7d2fe; background: #eef2ff; color: #4338ca; font-weight: 800; font-size: .74rem;
            padding: 4px 10px; border-radius: 10px; cursor: pointer; display: inline-flex; align-items: center; gap: 5px; transition: all .2s; }
        .dcp-split:hover { background: #e0e7ff; }
        .dcp-chev { transition: transform .2s; font-size: .65rem; }
        .dcp-split.open .dcp-chev { transform: rotate(180deg); }
        .dcp-parts td { background: #f8faff !important; padding: 6px 14px 12px !important; }
        .dcp-parts-list { display: flex; flex-direction: column; gap: 6px; animation: dcpFade .25s ease both; }
        .dcp-part { display: flex; align-items: center; gap: 10px; background: #fff; border: 1px solid #e0e7ff; border-radius: 10px;
            padding: 6px 12px; font-size: .8rem; }
        .dcp-part-dot { width: 8px; height: 8px; border-radius: 50%; background: #0d9488; flex-shrink: 0; }
        .dcp-part.arrears .dcp-part-dot { background: #6366f1; }
        .dcp-part-visit { color: #334155; font-weight: 700; }
        .dcp-part-tag { font-size: .66rem; font-weight: 800; padding: 2px 8px; border-radius: 8px; background: #f0fdfa; color: #0f766e; }
        .dcp-part.arrears .dcp-part-tag { background: #eef2ff; color: #4338ca; }
        .dcp-part-amt { margin-inline-start: auto; color: #047857; }

        .dcp-pv-empty, .dcp-pv-warn { font-size: .8rem; font-weight: 700; color: #94a3b8; padding: 10px 12px; border: 1.5px dashed #e2e8f0;
            border-radius: 12px; display: flex; gap: 6px; align-items: center; }
        .dcp-pv-warn { color: #b91c1c; border-color: #fecaca; background: #fef2f2; border-style: solid; }
        .dcp-pv { border: 1.5px solid #ccf3ec; background: linear-gradient(180deg, #f0fdfa, #fff); border-radius: 14px; padding: 10px 12px;
            display: flex; flex-direction: column; gap: 6px; animation: dcpFade .2s ease both; }
        .dcp-pv-head { font-size: .76rem; font-weight: 800; color: #0f766e; display: flex; gap: 6px; align-items: center; }
        .dcp-pv-line { display: flex; justify-content: space-between; align-items: center; gap: 10px; font-size: .82rem;
            background: #fff; border-radius: 10px; padding: 6px 10px; border: 1px solid #eef2f6; }
        .dcp-pv-line span { color: #334155; font-weight: 700; display: flex; gap: 6px; align-items: center; }
        .dcp-pv-line b { color: #047857; white-space: nowrap; }
        .dcp-pv-line b small { color: #b45309; font-weight: 800; }
        .dcp-pv-line.arrears span i { color: #6366f1; }
        .dcp-pv-line.credit { border-color: #fde68a; background: #fffbeb; }
        .dcp-pv-line.credit b, .dcp-pv-line.credit span { color: #b45309; }
        .dcp-pv-foot { display: flex; justify-content: space-between; font-size: .78rem; font-weight: 800; color: #475569;
            border-top: 1px dashed #ccf3ec; padding-top: 6px; margin-top: 2px; }
        .dcp-pv-foot b { color: #dc2626; }`;
    document.head.appendChild(css);

    return { receiptRows, toggle, allocate, renderPreview };
})();
