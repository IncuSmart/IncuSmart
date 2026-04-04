using System.ComponentModel.DataAnnotations;

namespace IncuSmart.API.Requests;

public class CreateControlDeviceRequest
{
    [Required(ErrorMessage = "MasterboardId is required")]
    public Guid MasterboardId { get; set; }

    public Guid? ControlBoardTypesId { get; set; }

    [Required(ErrorMessage = "ConfigId is required")]
    public Guid ConfigId { get; set; }

    [MaxLength(50, ErrorMessage = "HardwareCode must be at most 50 characters")]
    public string? HardwareCode { get; set; }

    public int? PinNumber { get; set; }

    [MaxLength(20, ErrorMessage = "State must be at most 20 characters")]
    public string? State { get; set; }
}
