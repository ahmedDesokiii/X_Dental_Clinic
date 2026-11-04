// ============================================================
// 📁 File: Modules/Clinical/Services/VisitNoteService.cs  (ملف جديد — المرحلة 2)
// ملاحظات المتابعة — بدون أي تعديل على الداتابيز:
// بتتخزن جوه Visits.Examination بصيغة JSON (شوف VisitNotesCodec).
// بتتحفظ فورًا (مش مستنية FinishSession) لأنها مستقلة عن الحسابات والشارت،
// ومينفعش تضيع لو الدكتور قفل السيشن من غير إنهاء.
// ============================================================
using Dental.Core.Results;
using Dental.Data;
using Dental.Modules.Clinical.DTOs;
using Dental.Modules.Clinical.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Dental.Modules.Clinical.Services
{
    public class VisitNoteService : IVisitNoteService
    {
        private const int MaxLength = 2000;
        private const string Marker = "\"dcNotes\"";
        private readonly ApplicationDbContext _context;

        public VisitNoteService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<VisitNoteDto>> GetByPatientAsync(int patientId)
        {
            // بس الزيارات اللي فيها ملاحظات بالصيغة الجديدة (LIKE على العمود)
            var visits = await _context.Visits
                .AsNoTracking()
                .Where(v => v.PatientId == patientId && v.Examination != null && v.Examination.Contains(Marker))
                .Select(v => new { v.Id, v.VisitDate, v.Examination })
                .ToListAsync();

            var raw = visits
                .SelectMany(v => VisitNotesCodec.Parse(v.Examination).Items
                    .Select(i => new { Visit = v, Item = i }))
                .ToList();

            if (raw.Count == 0)
                return new List<VisitNoteDto>();

            var linkedIds = raw.Where(x => x.Item.LinkedVisitDiagnosisId.HasValue)
                               .Select(x => x.Item.LinkedVisitDiagnosisId!.Value)
                               .Distinct()
                               .ToList();

            var linked = linkedIds.Count == 0
                ? new Dictionary<int, (int Tooth, string Name, DateTime Date)>()
                : (await _context.VisitDiagnoses
                    .AsNoTracking()
                    .Where(d => linkedIds.Contains(d.Id))
                    .Select(d => new { d.Id, Tooth = d.ToothNumber ?? 0, Name = d.Diagnosis.NameAr, Date = d.Visit.VisitDate })
                    .ToListAsync())
                  .ToDictionary(d => d.Id, d => (d.Tooth, d.Name, d.Date));

            return raw
                .Select(x =>
                {
                    var hasLink = x.Item.LinkedVisitDiagnosisId.HasValue
                                  && linked.ContainsKey(x.Item.LinkedVisitDiagnosisId.Value);
                    var l = hasLink ? linked[x.Item.LinkedVisitDiagnosisId!.Value] : default;
                    return new VisitNoteDto
                    {
                        Id = x.Item.Id,
                        VisitId = x.Visit.Id,
                        VisitDate = x.Visit.VisitDate,
                        CreatedAt = x.Item.CreatedAt,
                        ModifiedAt = x.Item.ModifiedAt,
                        Text = x.Item.Text,
                        LinkedVisitDiagnosisId = hasLink ? x.Item.LinkedVisitDiagnosisId : null,
                        LinkedToothNumber = hasLink ? l.Tooth : 0,
                        LinkedDiagnosisName = hasLink ? l.Name : null,
                        LinkedDiagnosisDate = hasLink ? l.Date : null
                    };
                })
                .OrderByDescending(n => n.CreatedAt)
                .ToList();
        }

        public async Task<List<LinkableDiagnosisDto>> GetLinkableDiagnosesAsync(int patientId)
        {
            var notesCount = (await GetByPatientAsync(patientId))
                .Where(n => n.LinkedVisitDiagnosisId.HasValue)
                .GroupBy(n => n.LinkedVisitDiagnosisId!.Value)
                .ToDictionary(g => g.Key, g => g.Count());

            var list = await _context.VisitDiagnoses
                .AsNoTracking()
                .Where(d => d.Visit.PatientId == patientId && !d.IsDeleted && d.Visit.IsActive)
                .OrderByDescending(d => d.Visit.VisitDate)
                .ThenBy(d => d.ToothNumber)
                .Select(d => new LinkableDiagnosisDto
                {
                    Id = d.Id,
                    VisitId = d.VisitId,
                    Date = d.Visit.VisitDate,
                    ToothNumber = d.ToothNumber ?? 0,
                    DiagnosisName = d.Diagnosis.NameAr
                })
                .ToListAsync();

            foreach (var d in list)
                d.NotesCount = notesCount.TryGetValue(d.Id, out var c) ? c : 0;

            return list;
        }

        public async Task<ServiceResult<VisitNoteDto>> SaveAsync(SaveVisitNoteRequest req)
        {
            var text = (req.Text ?? string.Empty).Trim();
            if (text.Length == 0)
                return ServiceResult<VisitNoteDto>.Fail("اكتب الملاحظة الأول.");
            if (text.Length > MaxLength)
                return ServiceResult<VisitNoteDto>.Fail($"الملاحظة أطول من {MaxLength} حرف.");

            var visit = await _context.Visits.FirstOrDefaultAsync(v => v.Id == req.VisitId);
            if (visit == null)
                return ServiceResult<VisitNoteDto>.Fail("الزيارة غير موجودة.");

            // التشخيص المربوط لازم يكون لنفس المريض
            if (req.LinkedVisitDiagnosisId.HasValue)
            {
                var ok = await _context.VisitDiagnoses.AnyAsync(d =>
                    d.Id == req.LinkedVisitDiagnosisId.Value && !d.IsDeleted && d.Visit.PatientId == visit.PatientId);
                if (!ok)
                    return ServiceResult<VisitNoteDto>.Fail("التشخيص المختار غير موجود لهذا المريض.");
            }

            var env = VisitNotesCodec.Parse(visit.Examination);
            VisitNotesCodec.Item item;

            if (!string.IsNullOrWhiteSpace(req.Id))
            {
                var existing = env.Items.FirstOrDefault(i => i.Id == req.Id);
                if (existing == null)
                    return ServiceResult<VisitNoteDto>.Fail("الملاحظة غير موجودة.");
                existing.Text = text;
                existing.LinkedVisitDiagnosisId = req.LinkedVisitDiagnosisId;
                existing.ModifiedAt = DateTime.Now;
                item = existing;
            }
            else
            {
                item = new VisitNotesCodec.Item
                {
                    Text = text,
                    LinkedVisitDiagnosisId = req.LinkedVisitDiagnosisId,
                    CreatedAt = DateTime.Now   // نفس توقيت VisitDate (محلي) عشان العرض والتجميع باليوم
                };
                env.Items.Add(item);
            }

            visit.Examination = VisitNotesCodec.Serialize(env);
            await _context.SaveChangesAsync();

            var dto = (await GetByPatientAsync(visit.PatientId)).First(n => n.Id == item.Id);
            return ServiceResult<VisitNoteDto>.Ok(dto, "تم حفظ الملاحظة.");
        }

        public async Task<ServiceResult<bool>> DeleteAsync(int visitId, string id)
        {
            var visit = await _context.Visits.FirstOrDefaultAsync(v => v.Id == visitId);
            if (visit == null)
                return ServiceResult<bool>.Fail("الزيارة غير موجودة.");

            var env = VisitNotesCodec.Parse(visit.Examination);
            var removed = env.Items.RemoveAll(i => i.Id == id);
            if (removed == 0)
                return ServiceResult<bool>.Fail("الملاحظة غير موجودة.");

            visit.Examination = VisitNotesCodec.Serialize(env);
            await _context.SaveChangesAsync();
            return ServiceResult<bool>.Ok(true, "تم حذف الملاحظة.");
        }
    }
}
