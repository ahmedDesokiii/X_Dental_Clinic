// ============================================================
// 📁 File: Modules/Clinical/Interfaces/IVisitNoteService.cs  (ملف جديد — المرحلة 2)
// ============================================================
using Dental.Core.Results;
using Dental.Modules.Clinical.DTOs;

namespace Dental.Modules.Clinical.Interfaces
{
    public interface IVisitNoteService
    {
        Task<List<VisitNoteDto>> GetByPatientAsync(int patientId);
        Task<List<LinkableDiagnosisDto>> GetLinkableDiagnosesAsync(int patientId);
        Task<ServiceResult<VisitNoteDto>> SaveAsync(SaveVisitNoteRequest req);
        Task<ServiceResult<bool>> DeleteAsync(int visitId, string id);
    }
}
