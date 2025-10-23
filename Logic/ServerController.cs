
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace Logic
{
    public class ServerController
    {



        private SynchronizationContext ctx;


        public Action<string> LogEntryEvent;
        public Action<string> MessageGetEvent;
        public Action<string> ServerLogEvent;


        public ServerController(SynchronizationContext ctx = null)
        {

            if (ctx != null)
            {
                this.ctx = ctx;
            }
        }

        public void LogSystem(string msg)
        {
            if (ctx != null)

                ctx.Post(d => ServerLogEvent?.Invoke(msg), null);
            else
                ServerLogEvent?.Invoke(msg);

        }
        public void LogMessage()
        {

        }

        private IPAddress ParceAdress(string ip)
        {
            IPAddress ip_ = IPAddress.Parse(ip);
            return ip_;
        }

        public async Task StartServer(string ip = "239.0.0.1", int port = 8888)
        {


        }
    }
}
