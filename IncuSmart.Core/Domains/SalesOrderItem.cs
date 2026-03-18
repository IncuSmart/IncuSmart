using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IncuSmart.Core.Domains
{
    public class SalesOrderItem : BaseDomain<BaseStatus>
    {
        public Guid OrderId { get; set; }
        public Guid IncubatorModelId { get; set; }
        public Guid? IncubatorId { get; set; }
    }

}
