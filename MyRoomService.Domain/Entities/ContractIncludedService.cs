using System.ComponentModel.DataAnnotations.Schema;

namespace MyRoomService.Domain.Entities
{
    public class ContractIncludedService
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }

        public Guid ContractId { get; set; }
        public virtual Contract? Contract { get; set; }

        public Guid UnitServiceId { get; set; }  // ✅ Direct link
        public virtual UnitService? UnitService { get; set; }  // ✅ Navigation

        // Optional: Allow contract-specific pricing override
        [Column(TypeName = "decimal(10, 2)")]
        public decimal? OverrideAmount { get; set; }
    }
}