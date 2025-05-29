using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logistiq.Domain.Common
{
    public interface ITenantEntity
    {
        Guid CompanyId { get; set; }
    }
}
