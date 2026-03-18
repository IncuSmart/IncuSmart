using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IncuSmart.Infra.Persistences.Entities
{
    [Table("control_devices")]
    public class ControlDeviceEntity : BaseEntity<BaseStatus>
    {
        public Guid MasterboardId { get; set; }
        public Guid? ControlBoardTypesId { get; set; }
        public Guid ConfigId { get; set; }
        public string? HardwareCode { get; set; }
        public int? PinNumber { get; set; }
        public string? State { get; set; }
    }
}
