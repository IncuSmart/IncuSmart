using System.ComponentModel.DataAnnotations.Schema;
using IncuSmart.Core.Enums;
using IncuSmart.Infra.Persistences.Entities.Base;

namespace IncuSmart.Infra.Persistences.Entities;

[Table("control_devices")]
public class ControlDeviceEntity : BaseEntity<BaseStatus>
{
    [ForeignKey(nameof(Masterboard))]
    public Guid MasterboardId { get; set; }
    public virtual MasterboardEntity Masterboard { get; set; } = null!;

    [ForeignKey(nameof(ControlBoardType))]
    public Guid? ControlBoardTypesId { get; set; }
    public virtual ControlBoardTypeEntity? ControlBoardType { get; set; }

    [ForeignKey(nameof(Config))]
    public Guid ConfigId { get; set; }
    public virtual ConfigEntity Config { get; set; } = null!;

    [Column(TypeName = "character varying(50)")]
    public string? HardwareCode { get; set; }

    public int? PinNumber { get; set; }

    [Column(TypeName = "character varying(20)")]
    public string? State { get; set; }
}
