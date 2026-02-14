namespace MyRoomService.Domain.Interfaces
{
    public interface IMustHaveTenant
    {
        Guid TenantId { get; set; }
    }
}
