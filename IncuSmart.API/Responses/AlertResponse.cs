using IncuSmart.Core.Domains;

namespace IncuSmart.API.Responses;

public class AlertResponse
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid IncubatorId { get; set; }
    public Incubator Incubator { get; set; }
    public Guid? ConfigId { get; set; }
    public IncubatorConfigInstance? Config { get; set; }
    public Guid? SensorId { get; set; }
    public Sensor? Sensor { get; set; }
    public decimal? Value { get; set; }
    public string? Severity { get; set; }
    public string? Message { get; set; }
    public string? ResolvedBy { get; set; }
    public Guid? ResolvedMlId { get; set; }
    public MlModel? MlModel { get; set; }
}