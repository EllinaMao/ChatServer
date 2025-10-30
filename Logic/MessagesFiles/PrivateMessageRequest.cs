using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logic.MessagesFiles
{
    // Клиент -> Сервер: Запрос на отправку PM (зачем? Что б я не могла отправить типа я админ личное сообщение. Почему не так как на уроке? Мы просто обговаривали что все через сервер должно идти, вот я так и делала)
    public class PrivateMessageRequest : TcpMessage
    {
        public PrivateMessageRequest() { Type = "PrivateMessage"; }
        public string ToUser { get; set; } // Кому
        public string Message { get; set; }  // Что
    }
}
