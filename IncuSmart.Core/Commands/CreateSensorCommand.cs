namespace IncuSmart.Core.Commands
{
    public class CreateSensorCommand
    {
        public Guid    MasterboardId     { get; set; }
        public Guid    ConfigInstanceId  { get; set; }
        public string? HardwareCode      { get; set; }
        public int?    PinNumber         { get; set; }
    }
}
