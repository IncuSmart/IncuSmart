using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IncuSmart.Core.Enums
{
    public enum IncubatorStatus
    {
        PENDING,
        PENDING_CLAIM,
        ACTIVE,
        INACTIVE,
        MAINTENANCE,
        DAMAGED,
        RETIRED
    }
}
