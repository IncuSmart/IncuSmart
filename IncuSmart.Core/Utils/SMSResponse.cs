
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace IncuSmart.Core.Utils
{
    public class SMSResponse
    {
        [JsonPropertyName("CodeResult")]
        public string CodeResult { get; set; } = string.Empty;  

        [JsonPropertyName("CountRegenerate")]
        public int CountRegenerate { get; set; }

        [JsonPropertyName("SMSID")]
        public string SMSID { get; set; } = string.Empty;
    }

}
