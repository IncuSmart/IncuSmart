using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IncuSmart.Core.Domains
{
    public class ControlDevice : BaseDomain<BaseStatus>
    {
        public Guid MasterboardId { get; set; }
        public Guid? ControlBoardTypesId { get; set; }
        public Guid ConfigId { get; set; }
        public string? HardwareCode { get; set; }
        public int? PinNumber { get; set; }
        public string? State { get; set; }
    }

}
