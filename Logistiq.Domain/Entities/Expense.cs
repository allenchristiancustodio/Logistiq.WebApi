using Logistiq.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logistiq.Domain.Entities
{
    public class Expense : BaseEntity, ITenantEntity
    {
        public Guid CompanyId { get; set; }
        public Guid CategoryId { get; set; }
        public decimal Amount { get; set; }
        public string? Description { get; set; }
        // Navigation
        public virtual ExpenseCategory Category { get; set; } = null!;
    }
}
