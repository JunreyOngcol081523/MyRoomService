namespace MyRoomService.Domain.Entities
{
    public class Tenant
    {
        public Guid Id { get; set; } // Changed to Guid
        public string Name { get; set; } = string.Empty;

        // Matches 'subscription_status' in schema
        public string SubscriptionStatus { get; set; } = "ACTIVE";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<Building> Buildings { get; set; } = new List<Building>();
    }
}