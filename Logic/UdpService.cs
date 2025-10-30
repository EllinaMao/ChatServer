using Logic.MessagesFiles;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Logic
{
    public class UdpService
    {
        private readonly UdpClient _udpListener;
        private readonly ChatHistoryManager _historyManager;
        private readonly Action<string, string> _logMessages; // Делегат для LogMessages
        private readonly Action<string> _logSystem;

        private const int UDP_PORT = 8001;
        private readonly IPAddress MULTICAST_GROUP = IPAddress.Parse("239.0.0.1");

        public UdpService(ChatHistoryManager historyManager, Action<string, string> logMessages, Action<string> logSystem)
        {
            _historyManager = historyManager;
            _logMessages = logMessages;
            _logSystem = logSystem;

            _udpListener = new UdpClient();
            _udpListener.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            _udpListener.Client.Bind(new IPEndPoint(IPAddress.Any, UDP_PORT));
            _udpListener.JoinMulticastGroup(MULTICAST_GROUP, IPAddress.Any);
        }

        public async Task StartListeningAsync()
        {
            while (true) // TODO: Добавить CancellationToken для остановки
            {
                try
                {
                    var result = await _udpListener.ReceiveAsync();
                    var json = Encoding.UTF8.GetString(result.Buffer);
                    var message = Message.FromJson(json);

                    _historyManager.AddMessage(message);

                    // Вызываем делегат, который мы получили от ServerController
                    _logMessages(message.Name, message.Msg);
                }
                catch (ObjectDisposedException)
                {
                    // Ожидаемое исключение при остановке
                    break;
                }
                catch (Exception ex)
                {
                    _logSystem($"Ошибка UDP: {ex.Message}");
                }
            }
        }

        public void Stop()
        {
            _udpListener?.Close();
            _udpListener?.Dispose();
        }
    }
}