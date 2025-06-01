using Logistiq.Domain.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logistiq.Domain.Entities
{
    public class ExpenseCategory : BaseEntity, IOrganizationEntity
    {
        public string ClerkOrganizationId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public virtual Organization Organization { get; set; } = null!;
        public virtual ICollection<Expense> Expenses { get; set; } = new List<Expense>();
    }
}
