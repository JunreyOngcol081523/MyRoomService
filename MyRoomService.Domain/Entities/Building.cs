using MyRoomService.Domain.Entities;
using MyRoomService.Domain.Interfaces;

public class Building : IMustHaveTenant
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }

    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }

    // This can stay an Enum in C#; EF Core will save it as a string to match the schema
    public ResidentialType BuildingType { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Unit> Units { get; set; } = new List<Unit>();
    public bool IsArchived { get; set; } = false;
}