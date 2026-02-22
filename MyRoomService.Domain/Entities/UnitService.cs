using MyRoomService.Domain.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyRoomService.Domain.Entities
{
    public class UnitService : IMustHaveTenant
    {
        // Changed to Guid to match the new schema standard
        public Guid Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty;

        [DataType(DataType.Currency)]
        // Updated to (10, 2) to match your co-developer's SQL decimal precision
        [Column(TypeName = "decimal(10, 2)")]
        public decimal MonthlyPrice { get; set; }

        // Links this service to a specific Unit using Guid
        public Guid UnitId { get; set; }
        public Unit? Unit { get; set; }
        // Changed to Guid to match IMustHaveTenant refactor
        public Guid TenantId { get; set; }
    }
}