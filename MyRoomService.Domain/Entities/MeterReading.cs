using MyRoomService.Domain.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace MyRoomService.Domain.Entities
{
    public class MeterReading : IMustHaveTenant
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }

        // Links to the specific utility defined in the Unit (e.g., "Electricity")
        [Required]
        public Guid UnitServiceId { get; set; }
        public UnitService? UnitService { get; set; }

        // Values are doubles to support decimal points in meter digits
        public double PreviousValue { get; set; }
        public double CurrentValue { get; set; }

        // Calculated property to simplify consumption logic
        public double Consumption => CurrentValue - PreviousValue;

        [Required]
        public DateTime ReadingDate { get; set; }

        // This flag ensures that once an invoice is generated for this reading, 
        // it won't be picked up again in the next billing cycle.
        public bool IsBilled { get; set; } = false;

        // Useful for the Landlord UI to see who recorded the reading
        public string? Notes { get; set; }
    }
}