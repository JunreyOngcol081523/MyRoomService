using MyRoomService.Domain.Interfaces;

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
        // Helps in dropdowns and lists
        public string FullName => $"{FirstName} {LastName}";
        public KycStatus KycStatus { get; set; } = KycStatus.Pending;

        // Navigation: One occupant can have multiple contracts over time
        public ICollection<Contract> Contracts { get; set; } = new List<Contract>();
        public string? IdentityUserId { get; set; } // Links to AspNetUsers.Id
        public bool IsArchived { get; set; } = false;
    }
    public enum KycStatus
    {
        Pending = 0,
        Verified = 1,
        Rejected = 2,
        Expired = 3
    }
}