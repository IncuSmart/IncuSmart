using IncuSmart.Core.Enums;

namespace IncuSmart.Infra.Persistences.Entities
{
    [Table("incubator_models")]
    public class IncubatorModelEntity : BaseEntity<BaseStatus>
    {
        public string? ModelCode { get; set; }

        public string? Name { get; set; }

        public string? Description { get; set; }
    }
}
