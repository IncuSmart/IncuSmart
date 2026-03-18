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
        public string? Action { get; set; }
        public string? Entity { get; set; }
        public Guid? EntityId { get; set; }
    }
}
