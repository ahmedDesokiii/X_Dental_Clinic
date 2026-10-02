using Dental.Models.Inventory;
using Dental.Modules.Inventory.DTOs;
using Dental.Modules.Inventory.InterfacesInventory;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;
using static Dental.Constants.EnumHelper;

namespace Dental.Controllers
{
    public class InventoryController : Controller
    {
        private readonly IInventoryService _inventoryService;
        private readonly ApplicationDbContext _context;

        public InventoryController(IInventoryService inventoryService, ApplicationDbContext context)
        {
            _inventoryService = inventoryService;
            _context = context;
        }

        private string CurrentUser => User?.Identity?.Name ?? "System";

        [HttpGet]
        public IActionResult Index() => View();

        [HttpGet]
        public async Task<IActionResult> GetMaterials([FromQuery] MaterialFilterDto filter)
        {
            var data = await _inventoryService.GetFilteredAsync(filter);
            return Json(new { success = true, data });
        }


[HttpGet]
    public async Task<IActionResult> GetSuppliersLookup()
    {
        var suppliers = await _context.Suppliers
            .Where(s => s.IsActive)
            .OrderBy(s => s.Name)
            .Select(s => new { id = s.Id, name = s.Name })
            .ToListAsync();
        return Json(new { success = true, data = suppliers });
    }

    public class SupplierQuickDto
    {
        public string Name { get; set; } = null!;
        public string? Phone { get; set; }
}

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateSupplier([FromBody] SupplierQuickDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
                return Json(new { success = false, message = "اسم المورد مطلوب." });

            var supplier = new Supplier { Name = dto.Name.Trim(), Phone = dto.Phone, IsActive = true };
            _context.Suppliers.Add(supplier);
            await _context.SaveChangesAsync();

            return Json(new { success = true, data = new { id = supplier.Id, name = supplier.Name } });
        }

        [HttpGet]
        public async Task<IActionResult> GetDashboard()
        {
            var data = await _inventoryService.GetDashboardAsync();
            return Json(new { success = true, data });
        }

        [HttpGet]
        public async Task<IActionResult> Modal(int? id)
        {
            ViewBag.Suppliers = new SelectList(
                await _context.Suppliers.Where(s => s.IsActive).ToListAsync(), "Id", "Name");
            ViewBag.Categories = GetEnumSelectList<MaterialCategory>();
            ViewBag.Units = GetEnumSelectList<MaterialUnit>();
            ViewBag.IsEdit = id.HasValue && id.Value > 0;

            if (id.HasValue && id.Value > 0)
            {
                var result = await _inventoryService.GetByIdAsync(id.Value);
                if (!result.Success) return NotFound();
                return PartialView("_MaterialFormModal", result.Data);
            }

            return PartialView("_MaterialFormModal", new MaterialDetailsDto { Code = await GenerateNextCodeAsync() });
        }

        // ── توليد الكود التالي: M-1001, M-1002 ... ──
        private async Task<string> GenerateNextCodeAsync()
        {
            // IgnoreQueryFilters عشان الماتريال المحذوف (soft delete) يتحسب ومنكررش كوده
            var codes = await _context.Materials
                .IgnoreQueryFilters()
                .Where(m => m.Code.StartsWith("MT-"))
                .Select(m => m.Code)
                .ToListAsync();

            var max = codes
                .Select(c => Regex.Match(c, @"^MT-(\d+)$"))
                .Where(m => m.Success)
                .Select(m => int.Parse(m.Groups[1].Value))
                .DefaultIfEmpty(0)
                .Max();

            return $"MT-{Math.Max(max, 1000) + 1}";
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(MaterialFormDto dto)
        {
            if (dto.Id > 0)
            {
                // التعديل: الكود يفضل زي ما هو
                var existing = await _inventoryService.GetByIdAsync(dto.Id);
                if (!existing.Success)
                    return Json(new { success = false, message = existing.Message });
                dto.Code = existing.Data.Code;
            }
            else
            {
                // الإضافة: السيرفر هو اللي يولّد الكود
                dto.Code = await GenerateNextCodeAsync();
            }
            ModelState.Remove(nameof(dto.Code));

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return Json(new { success = false, message = string.Join(" | ", errors) });
            }

            if (dto.Id > 0)
            {
                var res = await _inventoryService.UpdateAsync(dto, CurrentUser);
                return Json(new { success = res.Success, message = res.Message });
            }

            var createRes = await _inventoryService.CreateAsync(dto, CurrentUser);
            return Json(new { success = createRes.Success, message = createRes.Message, id = createRes.Data });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _inventoryService.DeleteAsync(id, CurrentUser);
            return Json(new { success = result.Success, message = result.Message });
        }

        [HttpPost]
        public async Task<IActionResult> StockIn([FromBody] StockInRequest req)
        {
            var result = await _inventoryService.StockInAsync(req, CurrentUser);
            return Json(new { success = result.Success, message = result.Message });
        }

        [HttpPost]
        public async Task<IActionResult> StockOut([FromBody] StockOutRequest req)
        {
            var result = await _inventoryService.StockOutAsync(req, CurrentUser);
            return Json(new { success = result.Success, message = result.Message });
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var result = await _inventoryService.GetByIdAsync(id);
            if (!result.Success) { TempData["Error"] = result.Message; return RedirectToAction(nameof(Index)); }

            ViewBag.History = await _inventoryService.GetHistoryAsync(id);
            return View(result.Data);
        }

        [HttpGet]
        public async Task<IActionResult> GetHistory(int materialId)
        {
            var data = await _inventoryService.GetHistoryAsync(materialId);
            return Json(new { success = true, data });
        }
    }
}