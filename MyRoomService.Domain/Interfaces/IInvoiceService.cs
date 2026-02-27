using MyRoomService.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyRoomService.Domain.Interfaces
{
    public interface IInvoiceService
    {
        // Generates invoices for all active contracts whose billing day matches the target date
        Task<int> GenerateMonthlyInvoicesAsync(Guid tenantId, DateTime targetDate, bool autoPublish);

        // Generates an invoice for a specific contract (Great for manual overrides)
        Task<Invoice?> GenerateInvoiceForContractAsync(
            Guid tenantId,
            Guid contractId,
            DateTime targetDate,
            bool autoPublish = false,
            HashSet<Guid>? processedReadings = null); // 🚨 New parameter added here
    }
}