using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logic.MessagesFiles
{
    public class UserListMessage : TcpMessage
    {
        public UserListMessage() { Type = "UserList"; }
        public List<string> Users { get; set; }
    }
}
