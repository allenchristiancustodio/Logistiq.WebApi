using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logistiq.Domain.Enums
{
    public enum OrderType
    {
        Purchase = 1,
        Sale = 2,
        Transfer = 3,
        Return = 4,
        Adjustment = 5
    }
}
