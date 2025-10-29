using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Logic.MessagesFiles
{
    //[Serializable]
    public class Message
    {
        public string Msg { get; set; }
        public string Name { get; set; }
        public string ToJson()
        {
            return JsonSerializer.Serialize(this);
        }

        public static Message? FromJson(string json)
        {
            return JsonSerializer.Deserialize<Message>(json);
        }
    }
}
