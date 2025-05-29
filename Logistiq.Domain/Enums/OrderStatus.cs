using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logistiq.Domain.Enums
{
    public enum OrderStatus
    {
        Draft = 1,
        Pending = 2,
        Confirmed = 3,
        Processing = 4,
        Shipped = 5,
        Delivered = 6,
        Completed = 7,
        Cancelled = 8,
        Returned = 9
    }
}
