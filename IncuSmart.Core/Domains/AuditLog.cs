using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IncuSmart.Core.Domains
{
    public class AuditLog : BaseDomain<BaseStatus>
    {
        public Guid UserId { get; set; }
        public AuditAction Action { get; set; }
        public AuditEntityType Entity { get; set; }
        public Guid? EntityId { get; set; }
    }
}
