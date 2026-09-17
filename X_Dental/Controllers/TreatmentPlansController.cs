using Dental.Modules.Clinical.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static Dental.Constants.EnumHelper;

namespace Dental.Controllers
{
    public class TreatmentPlansController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<TreatmentPlansController> _logger;

        public TreatmentPlansController(ApplicationDbContext context, ILogger<TreatmentPlansController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ── Helpers ──
        private static (string text, string colorClass) PlanStatusMeta(TreatmentPlanStatus s) => s switch
        {
            TreatmentPlanStatus.Draft => ("Draft", "plan-badge-draft"),
            TreatmentPlanStatus.Approved => ("Approved", "plan-badge-approved"),
            TreatmentPlanStatus.InProgress => ("In Progress", "plan-badge-progress"),
            TreatmentPlanStatus.Completed => ("Completed", "plan-badge-completed"),
            TreatmentPlanStatus.Cancelled => ("Cancelled", "plan-badge-cancelled"),
            _ => (s.ToString(), "plan-badge-draft")
        };

        private static (string text, string colorClass) StepStatusMeta(TreatmentStepStatus s) => s switch
        {
            TreatmentStepStatus.Planned => ("Planned", "step-badge-planned"),
            TreatmentStepStatus.InProgress => ("In Progress", "step-badge-progress"),
            TreatmentStepStatus.Done => ("Done", "step-badge-done"),
            TreatmentStepStatus.Skipped => ("Skipped", "step-badge-skipped"),
            _ => (s.ToString(), "step-badge-planned")
        };

        private TreatmentPlanDto MapPlan(Models.Clinic.TreatmentPlan p)
        {
            var (statusText, statusColor) = PlanStatusMeta(p.Status);
            var steps = p.Steps
                .OrderBy(s => s.StepOrder)
                .Select(MapStep)
                .ToList();

            var total = steps.Count;
            var done = steps.Count(s => s.Status == TreatmentStepStatus.Done);

            return new TreatmentPlanDto
            {
                Id = p.Id,
                PatientId = p.PatientId,
                VisitId = p.VisitId,
                Title = p.Title,
                Notes = p.Notes,
                Status = p.Status,
                StatusText = statusText,
                StatusColorClass = statusColor,
                CreatedDate = p.CreatedDate,
                ApprovedDate = p.ApprovedDate,
                TotalEstimatedCost = steps.Sum(s => s.EstimatedCost),
                TotalSteps = total,
                CompletedSteps = done,
                ProgressPercent = total > 0 ? (int)Math.Round((double)done / total * 100) : 0,
                Steps = steps
            };
        }

        private TreatmentPlanStepDto MapStep(Models.Clinic.TreatmentPlanStep s)
        {
            var (statusText, statusColor) = StepStatusMeta(s.Status);
            return new TreatmentPlanStepDto
            {
                Id = s.Id,
                TreatmentPlanId = s.TreatmentPlanId,
                StepOrder = s.StepOrder,
                Description = s.Description,
                ProcedureCatalogId = s.ProcedureCatalogId,
                ProcedureName = s.ProcedureCatalog?.NameEn,
                ToothNumber = s.ToothNumber,
                EstimatedCost = s.EstimatedCost,
                TargetDate = s.TargetDate,
                Status = s.Status,
                StatusText = statusText,
                StatusColorClass = statusColor,
                CompletedVisitId = s.CompletedVisitId,
                CompletedDate = s.CompletedDate,
                Notes = s.Notes
            };
        }

        // ── GET: list plans for a patient ──
        [HttpGet]
        public async Task<IActionResult> GetPlans(int patientId)
        {
            var plans = await _context.TreatmentPlans
                .Include(p => p.Steps).ThenInclude(s => s.ProcedureCatalog)
                .Where(p => p.PatientId == patientId)
                .OrderByDescending(p => p.CreatedDate)
                .ToListAsync();

            var data = plans.Select(MapPlan).ToList();
            return Json(new { success = true, data });
        }

        // ── GET: procedure catalog for the "add step" dropdown ──
        [HttpGet]
        public async Task<IActionResult> GetProcedureOptions()
        {
            var list = await _context.DiagnosisCatalogs
                .Where(p => p.IsActive)
                .Select(p => new { id = p.Id, name = p.NameEn, price = p.Price })
                .ToListAsync();
            return Json(new { success = true, data = list });
        }

        // ── GET: visits list for linking (used in "mark done" flow) ──
        [HttpGet]
        public async Task<IActionResult> GetPatientVisits(int patientId)
        {
            var visits = await _context.Visits
                .Where(v => v.PatientId == patientId)
                .OrderByDescending(v => v.VisitDate)
                .Select(v => new { id = v.Id, date = v.VisitDate, status = v.Status.ToString() })
                .ToListAsync();
            return Json(new { success = true, data = visits });
        }

        // ── CREATE plan ──
        [HttpPost]
        public async Task<IActionResult> CreatePlan([FromBody] CreateTreatmentPlanRequest req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.Title))
                return Json(new { success = false, message = "Plan title is required." });

            if (req.PatientId <= 0)
                return Json(new { success = false, message = "Invalid patient." });

            var patientExists = await _context.Patients.AnyAsync(p => p.Id == req.PatientId && p.IsActive);
            if (!patientExists)
                return Json(new { success = false, message = "Patient not found." });

            var plan = new Models.Clinic.TreatmentPlan
            {
                PatientId = req.PatientId,
                VisitId = req.VisitId,
                Title = req.Title.Trim(),
                Notes = req.Notes,
                Status = TreatmentPlanStatus.Draft,
                CreatedDate = DateTime.Now,
                CreatedBy = User?.Identity?.Name ?? "System"
            };

            int order = 1;
            foreach (var s in req.Steps.Where(s => !string.IsNullOrWhiteSpace(s.Description)))
            {
                plan.Steps.Add(new Models.Clinic.TreatmentPlanStep
                {
                    StepOrder = order++,
                    Description = s.Description.Trim(),
                    ProcedureCatalogId = s.ProcedureCatalogId,
                    ToothNumber = s.ToothNumber,
                    EstimatedCost = s.EstimatedCost,
                    TargetDate = s.TargetDate,
                    Notes = s.Notes,
                    Status = TreatmentStepStatus.Planned
                });
            }

            _context.TreatmentPlans.Add(plan);
            await _context.SaveChangesAsync();

            var fresh = await _context.TreatmentPlans
                .Include(p => p.Steps).ThenInclude(s => s.ProcedureCatalog)
                .FirstAsync(p => p.Id == plan.Id);

            return Json(new { success = true, data = MapPlan(fresh) });
        }

        // ── ADD step to an existing plan ──
        [HttpPost]
        public async Task<IActionResult> AddStep([FromBody] AddStepRequest req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.Description))
                return Json(new { success = false, message = "Step description is required." });

            var plan = await _context.TreatmentPlans
                .Include(p => p.Steps)
                .FirstOrDefaultAsync(p => p.Id == req.TreatmentPlanId);

            if (plan == null)
                return Json(new { success = false, message = "Plan not found." });

            var nextOrder = (plan.Steps.Any() ? plan.Steps.Max(s => s.StepOrder) : 0) + 1;

            var step = new Models.Clinic.TreatmentPlanStep
            {
                TreatmentPlanId = plan.Id,
                StepOrder = nextOrder,
                Description = req.Description.Trim(),
                ProcedureCatalogId = req.ProcedureCatalogId,
                ToothNumber = req.ToothNumber,
                EstimatedCost = req.EstimatedCost,
                TargetDate = req.TargetDate,
                Notes = req.Notes,
                Status = TreatmentStepStatus.Planned
            };

            _context.TreatmentPlanSteps.Add(step);

            // ⭐ لو الخطة كانت Draft ولسه ما عندهاش خطوات، تفضل Draft.
            // لو كانت متكسّرة completed وضفنا خطوة جديدة، نرجعها InProgress
            if (plan.Status == TreatmentPlanStatus.Completed)
                plan.Status = TreatmentPlanStatus.InProgress;

            await _context.SaveChangesAsync();

            var savedStep = await _context.TreatmentPlanSteps
                .Include(s => s.ProcedureCatalog)
                .FirstAsync(s => s.Id == step.Id);

            return Json(new { success = true, data = MapStep(savedStep) });
        }

        // ── UPDATE step status ──
        [HttpPost]
        public async Task<IActionResult> UpdateStepStatus([FromBody] UpdateStepStatusRequest req)
        {
            var step = await _context.TreatmentPlanSteps
                .Include(s => s.TreatmentPlan).ThenInclude(p => p.Steps)
                .FirstOrDefaultAsync(s => s.Id == req.StepId);

            if (step == null)
                return Json(new { success = false, message = "Step not found." });

            step.Status = req.Status;
            step.CompletedVisitId = req.CompletedVisitId;
            step.CompletedDate = req.Status == TreatmentStepStatus.Done ? DateTime.Now : null;

            // ⭐ تحديث حالة الخطة تلقائيًا حسب حالة الخطوات
            var plan = step.TreatmentPlan;
            if (plan != null)
            {
                var allSteps = plan.Steps;
                if (allSteps.Any() && allSteps.All(s => s.Status == TreatmentStepStatus.Done || s.Status == TreatmentStepStatus.Skipped))
                {
                    plan.Status = TreatmentPlanStatus.Completed;
                }
                else if (allSteps.Any(s => s.Status == TreatmentStepStatus.Done || s.Status == TreatmentStepStatus.InProgress))
                {
                    if (plan.Status == TreatmentPlanStatus.Draft || plan.Status == TreatmentPlanStatus.Approved)
                        plan.Status = TreatmentPlanStatus.InProgress;
                }
            }

            await _context.SaveChangesAsync();

            var fresh = await _context.TreatmentPlanSteps
                .Include(s => s.ProcedureCatalog)
                .FirstAsync(s => s.Id == step.Id);

            return Json(new { success = true, data = MapStep(fresh), planStatus = plan?.Status.ToString() });
        }

        // ── UPDATE plan status manually (Approve / Cancel / etc.) ──
        [HttpPost]
        public async Task<IActionResult> UpdatePlanStatus([FromBody] UpdatePlanStatusRequest req)
        {
            var plan = await _context.TreatmentPlans.FindAsync(req.PlanId);
            if (plan == null)
                return Json(new { success = false, message = "Plan not found." });

            plan.Status = req.Status;
            if (req.Status == TreatmentPlanStatus.Approved && plan.ApprovedDate == null)
                plan.ApprovedDate = DateTime.Now;

            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        // ── DELETE step ──
        [HttpPost]
        public async Task<IActionResult> DeleteStep(int id)
        {
            var step = await _context.TreatmentPlanSteps.FindAsync(id);
            if (step == null)
                return Json(new { success = false, message = "Step not found." });

            _context.TreatmentPlanSteps.Remove(step);
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        // ── DELETE plan (with all steps) ──
        [HttpPost]
        public async Task<IActionResult> DeletePlan(int id)
        {
            var plan = await _context.TreatmentPlans
                .Include(p => p.Steps)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (plan == null)
                return Json(new { success = false, message = "Plan not found." });

            _context.TreatmentPlanSteps.RemoveRange(plan.Steps);
            _context.TreatmentPlans.Remove(plan);
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }

        // ── REORDER steps (drag & drop support, optional) ──
        [HttpPost]
        public async Task<IActionResult> ReorderSteps([FromBody] ReorderStepsRequest req)
        {
            var steps = await _context.TreatmentPlanSteps
                .Where(s => s.TreatmentPlanId == req.PlanId)
                .ToListAsync();

            for (int i = 0; i < req.OrderedStepIds.Count; i++)
            {
                var step = steps.FirstOrDefault(s => s.Id == req.OrderedStepIds[i]);
                if (step != null) step.StepOrder = i + 1;
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }
    }
}