namespace MyRoomService.HelperDTO
{
    public class UnitServiceInputModel
    {
        public Guid? Id { get; set; } // <--- ADD THIS LINE
        public string Name { get; set; } = string.Empty;
        public decimal MonthlyPrice { get; set; }
        public bool IsMetered { get; set; }
        public string? MeterNumber { get; set; }
    }
}
