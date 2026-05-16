using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IncuSmart.Core.Enums
{
    public enum IncubatorStatus
    {
        AVAILABLE,
        RESERVED,
        ACTIVE,
        REPLACEMENT_PENDING,
        IN_MAINTENANCE,
        DAMAGED,
        RETIRED
    }
}
