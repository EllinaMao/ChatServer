using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Logic.MessagesFiles
{
    public class ConnectMessage: TcpMessage
    {
        public ConnectMessage() { Type = "Connect"; }
        public string Name {  get; set; }

        public string ToJson()
        {
            return JsonSerializer.Serialize(this);
        }

        public static ConnectMessage? FromJson(string json)
        {
            return JsonSerializer.Deserialize<ConnectMessage>(json);
        }



    }
}
