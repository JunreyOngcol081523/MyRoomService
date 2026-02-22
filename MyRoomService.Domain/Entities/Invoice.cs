using MyRoomService.Domain.Interfaces;
using System.ComponentModel.DataAnnotations.Schema;

namespace MyRoomService.Domain.Entities
{
    public class Invoice : IMustHaveTenant
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }

        public Guid OccupantId { get; set; }
        public Occupant? Occupant { get; set; }

        // This MUST match the column in your ERD
        public Guid ContractId { get; set; }
        public Contract? Contract { get; set; }

        public DateTime InvoiceDate { get; set; }
        public DateTime DueDate { get; set; }

        [Column(TypeName = "decimal(10, 2)")]
        public decimal TotalAmount { get; set; }
        public decimal AmountPaid { get; set; } = 0;

        // Optional but helpful: A calculated property for what is still owed
        public decimal BalanceDue => TotalAmount - AmountPaid;
        public string Status { get; set; } = "UNPAID"; // UNPAID, PAID, PARTIAL, OVERDUE, VOID

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property for the line items
        public ICollection<InvoiceItem> Items { get; set; } = new List<InvoiceItem>();
    }
}