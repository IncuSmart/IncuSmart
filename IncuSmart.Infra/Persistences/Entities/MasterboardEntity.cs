using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IncuSmart.Infra.Persistences.Entities
{
    [Table("masterboards")]
    public class MasterboardEntity : BaseEntity<BaseStatus>
    {
        public Guid IncubatorId { get; set; }
        public string? SerialNumber { get; set; }
        public string? MacAddress { get; set; }
        public string? FirmwareVersion { get; set; }
        public DateTime? LastSeenAt { get; set; }
    }
}
