namespace MyRoomService.HelperDTO
{
    // --- NEW: DTO for catching the baseline services snapshot ---
    public class IncludedUnitServiceDto
    {
        public string Name { get; set; } = string.Empty;
        public decimal MonthlyPrice { get; set; }
    }
}
