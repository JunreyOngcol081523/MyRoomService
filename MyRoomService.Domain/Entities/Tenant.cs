namespace MyRoomService.Domain.Entities
{
    public class Tenant
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string SubscriptionPlan { get; set; } = "Free"; // e.g., Free, Basic, Premium

        // This links the Tenant to the Buildings
        public ICollection<Building> Buildings { get; set; } = new List<Building>();
    }
}
