namespace IncuSmart.Infra.Mqtt
{
    public class MqttOptions
    {
        public const string SectionName = "Mqtt";

        public string Host     { get; set; } = "";
        public int    Port     { get; set; } = 8883;
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
        public bool   UseTls   { get; set; } = true;
    }
}
