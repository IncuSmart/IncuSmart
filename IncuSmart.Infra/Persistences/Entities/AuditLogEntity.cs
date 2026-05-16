using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IncuSmart.Infra.Persistences.Entities
{
    [Table("audit_logs")]
    public class AuditLogEntity : BaseEntity<BaseStatus>
    {
        public Guid UserId { get; set; }
        public AuditAction Action { get; set; }
        public AuditEntityType Entity { get; set; }
        public Guid? EntityId { get; set; }
    }
}
