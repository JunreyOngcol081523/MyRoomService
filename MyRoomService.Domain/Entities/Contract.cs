using MyRoomService.Domain.Interfaces;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyRoomService.Domain.Entities
{
    public class Contract : IMustHaveTenant
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }

        public Guid OccupantId { get; set; }
        public virtual Occupant? Occupant { get; set; }  // ✅ Add virtual

        public Guid UnitId { get; set; }
        public virtual Unit? Unit { get; set; }  // ✅ Add virtual

        public ContractStatus Status { get; set; } = ContractStatus.Active;
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        [Column(TypeName = "decimal(10, 2)")]
        public decimal RentAmount { get; set; }

        public int BillingDay { get; set; } = 1;

        // ✅ ADD VIRTUAL - Critical for EF Core collection materialization
        public virtual ICollection<ContractAddOn> AddOns { get; set; } = new List<ContractAddOn>();
        public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
        public virtual ICollection<ContractIncludedService> IncludedServices { get; set; } = new List<ContractIncludedService>();
    }

    public enum ContractStatus
    {
        Active = 0,
        Ended = 1,
        Reserved = 2,
        Terminated = 3
    }
}