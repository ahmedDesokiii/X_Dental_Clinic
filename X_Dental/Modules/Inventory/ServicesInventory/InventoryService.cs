// ============================================================
// 📁 File: Modules/Inventory/ServicesInventory/InventoryService.cs
// استبدل الملف بالكامل — نفس اللوجيك، الرسائل والسجل الزمني بالإنجليزي
// ============================================================
using Dental.Core.Results;
using Dental.Data;
using Dental.Models.Inventory;
using Dental.Modules.Inventory.DTOs;
using Dental.Modules.Inventory.InterfacesInventory;
using Microsoft.EntityFrameworkCore;
using static Dental.Constants.EnumHelper;

namespace Dental.Modules.Inventory.ServicesInventory
{
    public class InventoryService : IInventoryService
    {
        private readonly ApplicationDbContext _context;
        public InventoryService(ApplicationDbContext context) => _context = context;

        private static (string text, string icon, string colorClass) ActionMeta(MaterialHistoryAction a) => a switch
        {
            MaterialHistoryAction.Created => ("Material created", "bi-plus-circle", "hist-created"),
            MaterialHistoryAction.Edited => ("Details updated", "bi-pencil-square", "hist-edited"),
            MaterialHistoryAction.StockIn => ("Stock received", "bi-box-arrow-in-down", "hist-in"),
            MaterialHistoryAction.StockOut => ("Stock issued", "bi-box-arrow-up", "hist-out"),
            MaterialHistoryAction.PriceChanged => ("Price changed", "bi-currency-exchange", "hist-price"),
            MaterialHistoryAction.QuantityAdjusted => ("Quantity adjusted", "bi-sliders", "hist-edited"),
            MaterialHistoryAction.ExpiryUpdated => ("Expiry date updated", "bi-calendar-x", "hist-expiry"),
            MaterialHistoryAction.Deleted => ("Deleted", "bi-trash3", "hist-deleted"),
            _ => (a.ToString(), "bi-circle", "hist-edited")
        };

        public async Task<List<MaterialListDto>> GetFilteredAsync(MaterialFilterDto filter)
        {
            var query = _context.Materials
                .AsNoTracking()
                .Include(m => m.Supplier)
                .Where(m => !m.IsDeleted)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(filter.Search))
            {
                var s = filter.Search.Trim();
                query = query.Where(m => m.Name.Contains(s) || m.Code.Contains(s));
            }
            if (filter.Category.HasValue)
                query = query.Where(m => m.Category == filter.Category.Value);
            if (filter.SupplierId.HasValue)
                query = query.Where(m => m.SupplierId == filter.SupplierId.Value);

            var list = await query.ToListAsync();

            if (filter.LowStock == true) list = list.Where(m => m.IsLowStock).ToList();
            if (filter.OutOfStock == true) list = list.Where(m => m.IsOutOfStock).ToList();
            if (filter.Expired == true) list = list.Where(m => m.IsExpired).ToList();
            if (filter.NearExpiry == true) list = list.Where(m => m.IsNearExpiry).ToList();
            if (filter.Available == true) list = list.Where(m => !m.IsOutOfStock && !m.IsExpired).ToList();

            return list.OrderBy(m => m.Name).Select(m => new MaterialListDto
            {
                Id = m.Id,
                Name = m.Name,
                Code = m.Code,
                CategoryName = m.Category.ToString(),
                UnitName = m.Unit.ToString(),
                Quantity = m.Quantity,
                MinimumStockLevel = m.MinimumStockLevel,
                CurrentPrice = m.CurrentPrice,
                SupplierName = m.Supplier?.Name,
                ExpiryDate = m.ExpiryDate,
                IsActive = m.IsActive,
                IsOutOfStock = m.IsOutOfStock,
                IsLowStock = m.IsLowStock,
                IsExpired = m.IsExpired,
                IsNearExpiry = m.IsNearExpiry
            }).ToList();
        }

        public async Task<ServiceResult<MaterialDetailsDto>> GetByIdAsync(int id)
        {
            var m = await _context.Materials
                .Include(x => x.Supplier)
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
            if (m == null)
                return ServiceResult<MaterialDetailsDto>.Fail("Material not found.");

            var dto = new MaterialDetailsDto
            {
                Id = m.Id,
                Name = m.Name,
                Code = m.Code,
                Category = m.Category,
                CategoryName = m.Category.ToString(),
                Description = m.Description,
                Unit = m.Unit,
                UnitName = m.Unit.ToString(),
                Quantity = m.Quantity,
                MinimumStockLevel = m.MinimumStockLevel,
                StorageLocation = m.StorageLocation,
                PurchasePrice = m.PurchasePrice,
                CurrentPrice = m.CurrentPrice,
                SupplierId = m.SupplierId,
                SupplierName = m.Supplier?.Name,
                Manufacturer = m.Manufacturer,
                BatchNumber = m.BatchNumber,
                ExpiryDate = m.ExpiryDate,
                IsActive = m.IsActive,
                Notes = m.Notes,
                IsOutOfStock = m.IsOutOfStock,
                IsLowStock = m.IsLowStock,
                IsExpired = m.IsExpired,
                IsNearExpiry = m.IsNearExpiry,
                CreatedDate = m.CreatedDate,
                ModifiedDate = m.ModifiedDate
            };
            return ServiceResult<MaterialDetailsDto>.Ok(dto);
        }

        public async Task<bool> ExistsCodeAsync(string code, int? excludeId = null)
        {
            var q = _context.Materials.Where(m => m.Code == code && !m.IsDeleted);
            if (excludeId.HasValue) q = q.Where(m => m.Id != excludeId.Value);
            return await q.AnyAsync();
        }

        public async Task<ServiceResult<int>> CreateAsync(MaterialFormDto dto, string userName)
        {
            if (await ExistsCodeAsync(dto.Code))
                return ServiceResult<int>.Fail("This material code is already in use.");
            if (dto.Quantity < 0 || dto.PurchasePrice < 0 || dto.CurrentPrice < 0)
                return ServiceResult<int>.Fail("Entered values are not valid.");

            var entity = new Material
            {
                Name = dto.Name.Trim(),
                Code = dto.Code.Trim(),
                Category = dto.Category,
                Description = dto.Description,
                Unit = dto.Unit,
                Quantity = dto.Quantity,
                MinimumStockLevel = dto.MinimumStockLevel,
                StorageLocation = dto.StorageLocation,
                PurchasePrice = dto.PurchasePrice,
                CurrentPrice = dto.CurrentPrice,
                SupplierId = dto.SupplierId,
                Manufacturer = dto.Manufacturer,
                BatchNumber = dto.BatchNumber,
                ExpiryDate = dto.ExpiryDate,
                IsActive = dto.IsActive,
                Notes = dto.Notes
            };
            _context.Materials.Add(entity);
            await _context.SaveChangesAsync();

            _context.MaterialHistories.Add(new MaterialHistory
            {
                MaterialId = entity.Id,
                Action = MaterialHistoryAction.Created,
                UserName = userName,
                QuantityAfter = entity.Quantity,
                PriceAfter = entity.CurrentPrice,
                BatchNumber = entity.BatchNumber,
                Notes = "Material created in inventory"
            });
            await _context.SaveChangesAsync();
            return ServiceResult<int>.Ok(entity.Id, "Material added successfully.");
        }

        public async Task<ServiceResult<bool>> UpdateAsync(MaterialFormDto dto, string userName)
        {
            var entity = await _context.Materials.FirstOrDefaultAsync(m => m.Id == dto.Id && !m.IsDeleted);
            if (entity == null)
                return ServiceResult<bool>.Fail("Material not found.");
            if (await ExistsCodeAsync(dto.Code, dto.Id))
                return ServiceResult<bool>.Fail("This material code is already in use.");
            if (dto.PurchasePrice < 0 || dto.CurrentPrice < 0)
                return ServiceResult<bool>.Fail("Price cannot be negative.");

            var oldPrice = entity.CurrentPrice;
            var oldQty = entity.Quantity;

            entity.Name = dto.Name.Trim();
            entity.Code = dto.Code.Trim();
            entity.Category = dto.Category;
            entity.Description = dto.Description;
            entity.Unit = dto.Unit;
            entity.MinimumStockLevel = dto.MinimumStockLevel;
            entity.StorageLocation = dto.StorageLocation;
            entity.PurchasePrice = dto.PurchasePrice;
            entity.CurrentPrice = dto.CurrentPrice;
            entity.SupplierId = dto.SupplierId;
            entity.Manufacturer = dto.Manufacturer;
            entity.BatchNumber = dto.BatchNumber;
            entity.ExpiryDate = dto.ExpiryDate;
            entity.IsActive = dto.IsActive;
            entity.Notes = dto.Notes;
            entity.ModifiedDate = DateTime.Now;

            _context.MaterialHistories.Add(new MaterialHistory
            {
                MaterialId = entity.Id,
                Action = oldPrice != entity.CurrentPrice ? MaterialHistoryAction.PriceChanged : MaterialHistoryAction.Edited,
                UserName = userName,
                PriceBefore = oldPrice,
                PriceAfter = entity.CurrentPrice,
                QuantityBefore = oldQty,
                QuantityAfter = entity.Quantity,
                Notes = "Material details updated"
            });
            await _context.SaveChangesAsync();
            return ServiceResult<bool>.Ok(true, "Material updated successfully.");
        }

        public async Task<ServiceResult<bool>> DeleteAsync(int id, string userName)
        {
            var entity = await _context.Materials.FirstOrDefaultAsync(m => m.Id == id && !m.IsDeleted);
            if (entity == null)
                return ServiceResult<bool>.Fail("Material not found.");

            entity.IsDeleted = true;
            entity.IsActive = false;
            entity.ModifiedDate = DateTime.Now;

            _context.MaterialHistories.Add(new MaterialHistory
            {
                MaterialId = entity.Id,
                Action = MaterialHistoryAction.Deleted,
                UserName = userName,
                Notes = "Material deleted (soft delete)"
            });
            await _context.SaveChangesAsync();
            return ServiceResult<bool>.Ok(true, "Material deleted successfully.");
        }

        public async Task<ServiceResult<bool>> StockInAsync(StockInRequest req, string userName)
        {
            if (req.Quantity <= 0)
                return ServiceResult<bool>.Fail("Quantity must be greater than zero.");

            var material = await _context.Materials.FirstOrDefaultAsync(m => m.Id == req.MaterialId && !m.IsDeleted);
            if (material == null)
                return ServiceResult<bool>.Fail("Material not found.");

            var qtyBefore = material.Quantity;
            material.Quantity += req.Quantity;
            if (req.UnitPrice.HasValue && req.UnitPrice.Value >= 0)
                material.PurchasePrice = req.UnitPrice.Value;
            if (!string.IsNullOrWhiteSpace(req.BatchNumber))
                material.BatchNumber = req.BatchNumber;
            if (req.ExpiryDate.HasValue)
                material.ExpiryDate = req.ExpiryDate;
            if (req.SupplierId.HasValue)
                material.SupplierId = req.SupplierId;

            var transaction = new StockTransaction
            {
                MaterialId = material.Id,
                Type = StockTransactionType.StockIn,
                Quantity = req.Quantity,
                UnitPrice = req.UnitPrice,
                BatchNumber = req.BatchNumber,
                ExpiryDate = req.ExpiryDate,
                SupplierId = req.SupplierId,
                UserName = userName,
                
                Notes = req.Notes
            };
            _context.StockTransactions.Add(transaction);
            await _context.SaveChangesAsync();

            _context.MaterialHistories.Add(new MaterialHistory
            {
                MaterialId = material.Id,
                Action = MaterialHistoryAction.StockIn,
                UserName = userName,
                QuantityBefore = qtyBefore,
                QuantityAfter = material.Quantity,
                BatchNumber = req.BatchNumber,
                ReferenceTransactionId = transaction.Id,
                Notes = req.Notes
            });
            await _context.SaveChangesAsync();
            return ServiceResult<bool>.Ok(true, "Stock received successfully.");
        }

        public async Task<ServiceResult<bool>> StockOutAsync(StockOutRequest req, string userName)
        {
            if (req.Quantity <= 0)
                return ServiceResult<bool>.Fail("Quantity must be greater than zero.");

            var material = await _context.Materials.FirstOrDefaultAsync(m => m.Id == req.MaterialId && !m.IsDeleted);
            if (material == null)
                return ServiceResult<bool>.Fail("Material not found.");
            if (req.Quantity > material.Quantity)
                return ServiceResult<bool>.Fail($"Requested quantity ({req.Quantity}) exceeds available stock ({material.Quantity}).");

            var qtyBefore = material.Quantity;
            material.Quantity -= req.Quantity;

            var transaction = new StockTransaction
            {
                MaterialId = material.Id,
                Type = StockTransactionType.StockOut,
                Quantity = req.Quantity,
                DestinationOrReason = req.DestinationOrReason,
                PatientId = req.PatientId,
                VisitId = req.VisitId,
                UserName = userName,
                Notes = req.Notes
            };
            _context.StockTransactions.Add(transaction);
            await _context.SaveChangesAsync();

            _context.MaterialHistories.Add(new MaterialHistory
            {
                MaterialId = material.Id,
                Action = MaterialHistoryAction.StockOut,
                UserName = userName,
                QuantityBefore = qtyBefore,
                QuantityAfter = material.Quantity,
                ReferenceTransactionId = transaction.Id,
                Notes = req.DestinationOrReason ?? req.Notes
            });
            await _context.SaveChangesAsync();
            return ServiceResult<bool>.Ok(true, "Stock issued successfully.");
        }

        public async Task<List<MaterialHistoryDto>> GetHistoryAsync(int materialId)
        {
            return await _context.MaterialHistories
                .Where(h => h.MaterialId == materialId)
                .OrderByDescending(h => h.ActionDate)
                .Select(h => new MaterialHistoryDto
                {
                    ActionText = ActionMeta(h.Action).text,
                    Icon = ActionMeta(h.Action).icon,
                    ColorClass = ActionMeta(h.Action).colorClass,
                    ActionDate = h.ActionDate,
                    UserName = h.UserName,
                    FieldName = h.FieldName,
                    OldValue = h.OldValue,
                    NewValue = h.NewValue,
                    QuantityBefore = h.QuantityBefore,
                    QuantityAfter = h.QuantityAfter,
                    PriceBefore = h.PriceBefore,
                    PriceAfter = h.PriceAfter,
                    Notes = h.Notes
                })
                .ToListAsync();
        }

        public async Task<InventoryDashboardDto> GetDashboardAsync()
        {
            var materials = await _context.Materials
                .Where(m => !m.IsDeleted)
                .ToListAsync();

            return new InventoryDashboardDto
            {
                TotalMaterials = materials.Count,
                TotalStockValue = materials.Sum(m => m.Quantity * m.CurrentPrice),
                LowStockCount = materials.Count(m => m.IsLowStock),
                ExpiringSoonCount = materials.Count(m => m.IsNearExpiry),
                ExpiredCount = materials.Count(m => m.IsExpired),
                OutOfStockCount = materials.Count(m => m.IsOutOfStock),
                StockByCategory = materials
                    .GroupBy(m => m.Category)
                    .Select(g => new CategoryStockDto
                    {
                        CategoryName = g.Key.ToString(),
                        Count = g.Count(),
                        TotalValue = g.Sum(m => m.Quantity * m.CurrentPrice)
                    }).ToList()
            };
        }
    }
}