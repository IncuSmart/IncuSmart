using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IncuSmart.Core.Utils
{
    public class SMSDto
    {
        public string Phone { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string Brandname { get; set; } = "";
        public string SmsType { get; set; } = "8";
        public string IsUnicode { get; set; } = "0";
        public string? CampaignId { get; set; }
        public string? RequestId { get; set; }
        public string? CallbackUrl { get; set; }
    }

}
