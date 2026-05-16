using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace IncuSmart.Infra.Persistences.Entities
{
    [Table("control_devices")]
    public class ControlDeviceEntity : BaseEntity<BaseStatus>
    {
        public Guid    MasterboardId       { get; set; }
        public Guid?   ControlBoardTypesId { get; set; }
        public Guid    ConfigId            { get; set; }
        public string? HardwareCode        { get; set; }
        public int?    PinNumber           { get; set; }
        public string? State               { get; set; }

        // Navigation properties
        [ForeignKey("MasterboardId")]
        public MasterboardEntity? Masterboard { get; set; }

        [ForeignKey("ControlBoardTypesId")]
        public ControlBoardTypeEntity? ControlBoardType { get; set; }

        [ForeignKey("ConfigId")]
        public ConfigEntity? Config { get; set; }
    }
}
