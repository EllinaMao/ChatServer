using Logic.MessagesFiles;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace Logic
{
    public class ClientSession
    {
        private readonly TcpClient _tcpUser;
        private readonly UsersManager _userManager;
        private readonly ChatHistoryManager _historyManager;

        // способ сказать "серверу", что пора обновить списки.
        private readonly Func<Task> _broadcastUserListCallback;

        private NetworkStream _stream;
        private string? _userName;
        private readonly string _clientEndpoint;
        private readonly Action<string> _logSystem; // Делегат для логирования

        public ClientSession(TcpClient tcpUser,
                             UsersManager userManager,
                             ChatHistoryManager historyManager,
                             Func<Task> broadcastUserListCallback,
                             Action<string> logSystem)
        {
            _tcpUser = tcpUser;
            _userManager = userManager;
            _historyManager = historyManager;
            _broadcastUserListCallback = broadcastUserListCallback;
            _logSystem = logSystem; // Получаем метод LogSystem от сервера

            _clientEndpoint = tcpUser?.Client?.RemoteEndPoint?.ToString() ?? "unknown";
        }

        public async Task RunAsync()
        {
            try
            {
                _stream = _tcpUser.GetStream();
                byte[] buffer = new byte[4096];

                // --- ЭТАП 1: РЕГИСТРАЦИЯ ---
                int byteRead = await _stream.ReadAsync(buffer, 0, buffer.Length);
                if (byteRead == 0)
                {
                    _logSystem($"TCP: Клиент {_clientEndpoint} отвалился до регистрации.");
                    return;
                }

                string jsonRequest = Encoding.UTF8.GetString(buffer, 0, byteRead);

                // (Ваш код парсинга имени)
                var connectMsg = CommandMessage.FromJson(jsonRequest);
                _userName = connectMsg.Name;

                if (!_userManager.AddUser(_userName, _tcpUser))
                {
                    _logSystem($"TCP: Клиент {_clientEndpoint} попытался войти с занятым именем '{_userName}'.");
                    // TODO: Отправить клиенту "имя занято"
                    return;
                }

                _logSystem($"TCP: Клиент {_clientEndpoint} зарегистрирован как '{_userName}'.");

                // --- ЭТАП 2: ОТПРАВКА ИСТОРИИ И СПИСКА ---
                await SendHistoryAsync();

                // Вызываем callback, чтобы сервер обновил списки у ВСЕХ
                await _broadcastUserListCallback.Invoke();

                // --- ЭТАП 3: ЦИКЛ ЧТЕНИЯ PM ---
                while (_tcpUser.Connected) 
                {
                    byteRead = await _stream.ReadAsync(buffer, 0, buffer.Length);
                    if (byteRead == 0)
                    {
                        _logSystem($"TCP: Клиент '{_userName}' корректно отключился.");
                        break;
                    }

                    string clientJson = Encoding.UTF8.GetString(buffer, 0, byteRead);

                    // TODO: Обработать приватное сообщение.
                    // создать приватный метод ProcessPrivateMessage(clientJson)
                    // _logSystem($"PM от {_userName}: {clientJson}");
                }
            }
            catch (IOException ex)
            {
                _logSystem($"TCP: Клиент '{_userName}' ({_clientEndpoint}) отсоединен (IOException): {ex.Message}");
            }
            catch (Exception ex)
            {
                _logSystem($"TCP: Ошибка в ClientSession для '{_userName}': {ex.Message}");
            }
            finally
            {
                // --- ЭТАП 4: ОЧИСТКА ---
                _stream?.Close();
                _tcpUser?.Close();

                if (_userName != null)
                {
                    if (_userManager.RemoveUser(_userName))
                    {
                        _logSystem($"TCP: Клиент '{_userName}' удален из менеджера.");

                        // Снова вызываем callback, чтобы все узнали, что он ушел
                        await _broadcastUserListCallback.Invoke();
                    }
                }
                else
                {
                    _logSystem($"TCP: Клиент {_clientEndpoint} отсоединен (не успел зарегистрироваться).");
                }
            }
        }

        private async Task SendHistoryAsync()
        {
            List<Message> history = _historyManager.GetHistory();
            var historyMsg = new HistoryMessage { Messages = history };
            string jsonHistory = JsonSerializer.Serialize(historyMsg);
            byte[] historyBuffer = Encoding.UTF8.GetBytes(jsonHistory);

            await _stream.WriteAsync(historyBuffer, 0, historyBuffer.Length);

            _logSystem($"TCP: Отправлена история ({history.Count} сообщ.) клиенту '{_userName}'.");
        }
    }
}