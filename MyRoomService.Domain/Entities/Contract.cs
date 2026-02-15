using MyRoomService.Domain.Interfaces;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyRoomService.Domain.Entities
{
    public class Contract : IMustHaveTenant
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }

        public Guid OccupantId { get; set; }
        public Occupant? Occupant { get; set; }

        public Guid UnitId { get; set; }
        public Unit? Unit { get; set; }

        public string Status { get; set; } = "ACTIVE"; // ACTIVE, ENDED, RESERVED
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        [Column(TypeName = "decimal(10, 2)")]
        public decimal RentAmount { get; set; }

        public int BillingDay { get; set; } = 1; // Day of the month to generate invoice

        public ICollection<ContractAddOn> AddOns { get; set; } = new List<ContractAddOn>();
    }
}