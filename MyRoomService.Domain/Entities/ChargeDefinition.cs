using MyRoomService.Domain.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyRoomService.Domain.Entities
{
    public class ChargeDefinition : IMustHaveTenant
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty; // e.g., "Rice Cooker", "Internet"

        public string ChargeType { get; set; } = "RECURRING"; // RECURRING, ONE_TIME, METERED

        [Column(TypeName = "decimal(10, 2)")]
        public decimal DefaultAmount { get; set; }
    }
}