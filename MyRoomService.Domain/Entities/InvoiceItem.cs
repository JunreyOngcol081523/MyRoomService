using System.ComponentModel.DataAnnotations.Schema;

namespace MyRoomService.Domain.Entities
{
    public class InvoiceItem
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public Guid InvoiceId { get; set; }
        public Invoice? Invoice { get; set; }

        public string Description { get; set; } = string.Empty; // e.g., "Rent Oct 2024"

        [Column(TypeName = "decimal(10, 2)")]
        public decimal Amount { get; set; }

        // RENT, ADD_ON, UTILITY, PENALTY, DISCOUNT
        public string ItemType { get; set; } = "RENT";
    }
}