namespace IncuSmart.Infra.Persistences.Entities
{
    [Table("configs")]
    public class ConfigEntity : BaseEntity<BaseStatus>
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Type { get; set; }
        public string? Unit { get; set; }
        public string? Description { get; set; }
    }
}
