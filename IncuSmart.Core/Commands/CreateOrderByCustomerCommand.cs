using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IncuSmart.Core.Commands
{
    public class CreateOrderByCustomerCommand
    {
        public Guid UserId { get; set; }
        public List<OrderItemCommand> Items { get; set; } = new();
    }

}
