namespace MyRoomService.Domain.Entities
{
    public class Building
    {
        public int Id { get; set; } // The unique ID for the building
        public string Name { get; set; } // e.g., "Sunset Apartments"
        public string Address { get; set; } // Physical location

        // Multi-Tenancy: This links the building to a specific owner
        public int TenantId { get; set; }
        // The "Navigation" property
        public Tenant? Tenant { get; set; } = default!;
    }
}
