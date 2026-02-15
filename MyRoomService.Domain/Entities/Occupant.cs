using MyRoomService.Domain.Interfaces;
using System.Diagnostics.Contracts;

namespace MyRoomService.Domain.Entities
{
    public class Occupant : IMustHaveTenant
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }

        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;

        public string KycStatus { get; set; } = "PENDING";

        // Navigation: One occupant can have multiple contracts over time
        public ICollection<Contract> Contracts { get; set; } = new List<Contract>();
    }
}