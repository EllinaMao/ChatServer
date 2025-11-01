using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logic.MessagesFiles
{
    public class PrivateHistoryRequest : TcpMessage
    {
        public PrivateHistoryRequest() { Type = "GetPrivateHistory"; }
        public string WithUser { get; set; } // "Хочу историю с этим юзером"
    }
}
