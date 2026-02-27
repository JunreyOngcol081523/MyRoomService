using MyRoomService.Domain.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyRoomService.Domain.Entities
{
    public class Unit : IMustHaveTenant
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public Guid BuildingId { get; set; }
        public Building? Building { get; set; }

        [Required]
        public string UnitNumber { get; set; } = string.Empty; // Renamed from Number

        public int FloorLevel { get; set; }

        // ENTIRE_UNIT (Apartment) or PER_BED (Dorm)
        public string RentalMode { get; set; } = "ENTIRE_UNIT";

        public int MaxOccupancy { get; set; } = 1;

        [Column(TypeName = "decimal(10, 2)")]
        public decimal DefaultRate { get; set; } // Renamed from MonthlyRate

        public UnitStatus Status { get; set; } = UnitStatus.Available;
        public ICollection<Contract> Contracts { get; set; } = new List<Contract>();
        public ICollection<UnitService> UnitServices { get; set; } = new List<UnitService>();
        /// <summary>
        /// Defines how metered utility charges are distributed among occupants of this specific unit.
        /// </summary>
        [Required]
        public MeteredBillingMode MeteredBillingMode { get; set; } = MeteredBillingMode.SplitEqually;
    }
    public enum UnitStatus
    {
        Available = 0,
        Occupied = 1,
        Maintenance = 2,
        Reserved = 3
    }
}