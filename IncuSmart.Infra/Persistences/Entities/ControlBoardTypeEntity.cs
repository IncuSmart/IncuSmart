using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IncuSmart.Infra.Persistences.Entities
{
    [Table("control_board_types")]
    public class ControlBoardTypeEntity : BaseEntity<BaseStatus>
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
    }
}
