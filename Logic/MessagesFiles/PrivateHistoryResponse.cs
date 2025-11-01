using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logic.MessagesFiles
{
    public class PrivateHistoryResponse : TcpMessage
    {
        public PrivateHistoryResponse() { Type = "PrivateHistory"; }
        public string WithUser { get; set; }
        public List<Message> Messages { get; set; }
    }
}