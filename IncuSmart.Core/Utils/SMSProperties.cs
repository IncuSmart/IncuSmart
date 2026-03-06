using System.ComponentModel.DataAnnotations;

public class SMSProperties
{
    public const string SectionName = "SMS";

    [Required] public string ApiKey { get; set; } = string.Empty;
    [Required] public string SecretKey { get; set; } = string.Empty;
    [Required] public string Endpoint { get; set; } = string.Empty;

}

