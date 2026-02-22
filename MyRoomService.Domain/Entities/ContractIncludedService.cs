using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace MyRoomService.Domain.Entities
{
    public class ContractIncludedService
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }

        public Guid ContractId { get; set; }
        public Contract? Contract { get; set; }

        public string Name { get; set; } = string.Empty;

        [Column(TypeName = "decimal(10, 2)")]
        public decimal Amount { get; set; }
    }
}
