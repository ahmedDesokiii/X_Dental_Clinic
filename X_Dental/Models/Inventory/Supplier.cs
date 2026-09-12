using Dental.Models.Common;

namespace Dental.Models.Inventory
{
    public class Supplier : BaseEntity
    {
        public string Name { get; set; } = null!;
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
        public string? Notes { get; set; }
        public bool IsActive { get; set; } = true;
    }
}