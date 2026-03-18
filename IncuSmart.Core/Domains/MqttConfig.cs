using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IncuSmart.Core.Domains
{
    public class MqttConfig : BaseDomain<BaseStatus>
    {
        public int Port { get; set; }
        public string? BrokerAddress { get; set; }
        public int Qos { get; set; }
        public int KeepAlive { get; set; }
        public bool CleanSession { get; set; }
        public string? UserName { get; set; }
        public string? Password { get; set; }
        public string? WillMessage { get; set; }
        public bool UseTls { get; set; }
    }

}
