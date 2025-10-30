using Logic.MessagesFiles;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace Logic
{
    public class TcpService
    {
        private readonly TcpListener _tcpListener;
        private readonly UsersManager _userManager;
        private readonly ChatHistoryManager _historyManager;
        private readonly Action<string> _logSystem; // Делегат для LogSystem

        private const int TCP_PORT = 8002;
        private bool _isRunning = false;

        public TcpService(UsersManager userManager, ChatHistoryManager historyManager, Action<string> logSystem)
        {
            _userManager = userManager;
            _historyManager = historyManager;
            _logSystem = logSystem;

            _tcpListener = new TcpListener(IPAddress.Any, TCP_PORT);
        }

        public async Task StartListeningAsync()
        {
            _tcpListener.Start();
            _isRunning = true;
            _logSystem($"TCP: Слушаем на порту {TCP_PORT}...");

            while (_isRunning)
            {
                try
                {
                    TcpClient tcpClient = await _tcpListener.AcceptTcpClientAsync();
                    _logSystem($"TCP: Новый клиент {tcpClient.Client.RemoteEndPoint} подключился.");

                    // Создаем сессию
                    var session = new ClientSession(
                        tcpClient,
                        _userManager,
                        _historyManager,
                        BroadcastUserListAsync, // Передаем наш метод
                        _logSystem
                    );

                    _ = Task.Run(session.RunAsync);
                }
                catch (SocketException ex) when (ex.SocketErrorCode == SocketError.Interrupted)
                {
                    // Ожидаемое исключение при вызове Stop()
                    _isRunning = false;
                    _logSystem("TCP Listener остановлен.");
                }
                catch (Exception ex)
                {
                    _logSystem($"Критическая ошибка TCP Listener: {ex.Message}");
                }
            }
        }

        public void Stop()
        {
            _isRunning = false;
            _tcpListener?.Stop();
        }

        // Метод трансляции списка (SendAllUsersListAsync)
        private async Task BroadcastUserListAsync()
        {
            List<string> userNames = _userManager.GetAllUserNames();
            var userListMsg = new UserListMessage { Users = userNames };
            string json = JsonSerializer.Serialize(userListMsg);
            byte[] buffer = Encoding.UTF8.GetBytes(json);

            _logSystem($"SYSTEM: Список обновлен. {userNames.Count} пользователей онлайн.");

            // Используем _userManager, который у нас есть
            await _userManager.SendMessageAsync(buffer);
        }
    }
}