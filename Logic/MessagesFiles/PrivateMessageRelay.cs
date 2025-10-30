using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logic.MessagesFiles
{
    // Сервер -> Клиент: Доставка PM
    public class PrivateMessageRelay : TcpMessage
    {
        public PrivateMessageRelay() { Type = "PrivateMessage"; }
        public string FromUser { get; set; } // От кого
        public string Message { get; set; }   // Что
    }
}
