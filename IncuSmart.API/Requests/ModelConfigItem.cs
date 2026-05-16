using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IncuSmart.API.Requests
{
    public class ModelConfigItem
    {
        [Required(ErrorMessage = CommonConst.ConfigIdRequired)]
        public Guid ConfigId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = CommonConst.QuantityMustBeGreaterThanZeroValidation)]
        public int? Quantity { get; set; }

        public bool? Required { get; set; }

        public decimal? AbsoluteMin { get; set; }

        public decimal? AbsoluteMax { get; set; }
    }

}
