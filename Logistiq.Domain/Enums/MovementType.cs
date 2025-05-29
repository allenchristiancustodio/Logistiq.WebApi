using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logistiq.Domain.Enums
{
    public enum MovementType
    {
        StockIn = 1,
        StockOut = 2,
        Transfer = 3,
        Adjustment = 4,
        Return = 5,
        Damage = 6,
        Lost = 7
    }
}
