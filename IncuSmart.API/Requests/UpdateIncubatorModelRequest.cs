using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IncuSmart.API.Requests
{
    public class UpdateIncubatorModelRequest
    {
        [MaxLength(50, ErrorMessage = CommonConst.ModelCodeMaxLength)]
        public string? ModelCode { get; set; }

        [MinLength(2, ErrorMessage = CommonConst.NameMinLength)]
        [MaxLength(100, ErrorMessage = CommonConst.NameMaxLength)]
        public string? Name { get; set; }

        [MaxLength(255, ErrorMessage = CommonConst.DescriptionMaxLength255)]
        public string? Description { get; set; }

        [Range(1, long.MaxValue, ErrorMessage = CommonConst.UnitPriceMustBeGreaterThanZero)]
        public long? UnitPrice { get; set; }

        public List<ModelConfigItem>? Configs  { get; set; }
        public string?                ImageUrl { get; set; }
    }

}
