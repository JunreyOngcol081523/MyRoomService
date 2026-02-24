using System.ComponentModel.DataAnnotations.Schema;

namespace MyRoomService.Domain.Entities
{
    public class ContractAddOn
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public Guid ContractId { get; set; }
        public Contract? Contract { get; set; }

        public Guid ChargeDefinitionId { get; set; }
        public ChargeDefinition? ChargeDefinition { get; set; }

        [Column(TypeName = "decimal(10, 2)")]
        public decimal AgreedAmount { get; set; } // Can override the default price
        public bool IsProcessed { get; set; } = false;
    }
}