using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Logic.MessagesFiles
{
    public class CommandMessage
    {
        public string Type { get; set; }
        public string Name {  get; set; }

        public string ToJson()
        {
            return JsonSerializer.Serialize(this);
        }

        public static CommandMessage? FromJson(string json)
        {
            return JsonSerializer.Deserialize<CommandMessage>(json);
        }



    }
}
