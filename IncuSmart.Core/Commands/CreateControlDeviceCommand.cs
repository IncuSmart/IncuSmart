namespace IncuSmart.Core.Commands;

public class CreateControlDeviceCommand
{
    public Guid MasterboardId { get; set; }
    public Guid? ControlBoardTypesId { get; set; }
    public Guid ConfigId { get; set; }
    public string? HardwareCode { get; set; }
    public int? PinNumber { get; set; }
    public string? State { get; set; }
}
