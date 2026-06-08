namespace IncuSmart.API.Requests
{
    public class SetPowerRequest
    {
        [Required]
        public bool On { get; set; }
    }

    public class SetHeaterModeRequest
    {
        [Required]
        public string Mode { get; set; } = "";  // AUTO | MANUAL
    }

    public class SetFanModeRequest
    {
        [Required]
        public string Mode { get; set; } = "";  // AUTO | ON | OFF
    }

    public class SetFanSpeedRequest
    {
        [Required, Range(0, 100)]
        public int Speed { get; set; }
    }

    public class SetTemperatureRequest
    {
        [Required, Range(30.0, 40.0)]
        public double Value { get; set; }
    }
}
