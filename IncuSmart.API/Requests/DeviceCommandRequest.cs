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
}
