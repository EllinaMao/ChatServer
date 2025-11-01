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
        private readonly PrivateHistoryManager _privateHistoryManager;
        // способ сказать "серверу", что пора обновить списки.
        private readonly Func<Task> _broadcastUserListCallback;

        private NetworkStream _stream;
        private string? _userName;
        private readonly string _clientEndpoint;
        private readonly Action<string> _logSystem; // Делегат для логирования

        public ClientSession(TcpClient tcpUser,
                             UsersManager userManager,
                             ChatHistoryManager historyManager,
                             PrivateHistoryManager privateHistoryManager,
                             Func<Task> broadcastUserListCallback,
                             Action<string> logSystem)
        {
            _tcpUser = tcpUser;
            _userManager = userManager;
            _historyManager = historyManager;
            _broadcastUserListCallback = broadcastUserListCallback;
            _logSystem = logSystem; // Получаем метод LogSystem от сервера
            _privateHistoryManager = privateHistoryManager;

            _clientEndpoint = tcpUser?.Client?.RemoteEndPoint?.ToString() ?? "unknown";
        }

        public async Task RunAsync()
        {
            try
            {
                _stream = _tcpUser.GetStream();
                byte[] buffer = new byte[4096];

                // РЕГИСТРАЦИЯ
                int byteRead = await _stream.ReadAsync(buffer, 0, buffer.Length);
                if (byteRead == 0)
                {
                    _logSystem($"TCP: Клиент {_clientEndpoint} отвалился до регистрации.");
                    return;
                }

                string jsonRequest = Encoding.UTF8.GetString(buffer, 0, byteRead);

                // "Подсматриваем" тип
                var baseMsg = JsonSerializer.Deserialize<TcpMessage>(jsonRequest);

                // Проверяем, что это ПРАВИЛЬНОЕ первое сообщение
                if (baseMsg?.Type != "Connect")
                {
                    _logSystem($"TCP: Клиент {_clientEndpoint} отправил неверный первый пакет: '{baseMsg?.Type}'. Отключаем.");
                    return; // Завершаем сессию
                }

                // Теперь парсим в полный класс ConnectMessage
                var connectMsg = JsonSerializer.Deserialize<ConnectMessage>(jsonRequest);
                _userName = connectMsg.Name;
                //Проверяем, занято ли имя
                if (!_userManager.AddUser(_userName, _tcpUser))
                {
                    _logSystem($"TCP: Клиент {_clientEndpoint} попытался войти с занятым именем '{_userName}'.");

                    //Отправляем сообщение об ошибке
                    var errorMsg = new ErrorMessage { Reason = $"Имя '{_userName}' уже занято." };
                    string errorJson = JsonSerializer.Serialize(errorMsg);
                    byte[] errorBuffer = Encoding.UTF8.GetBytes(errorJson);
                    await _stream.WriteAsync(errorBuffer, 0, errorBuffer.Length);

                    // Закрываем соединение и выходим
                    return;
                }

                _logSystem($"TCP: Клиент {_clientEndpoint} зарегистрирован как '{_userName}'.");

                await SendHistoryAsync();

                await _broadcastUserListCallback.Invoke();

                // Чтение личных сообщений
                while (_tcpUser.Connected) 
                {
                    byteRead = await _stream.ReadAsync(buffer, 0, buffer.Length);
                    if (byteRead == 0)
                    {
                        _logSystem($"TCP: Клиент '{_userName}' корректно отключился.");
                        break;
                    }

                    string clientJson = Encoding.UTF8.GetString(buffer, 0, byteRead);

                    await ProcessTcpCommandAsync(clientJson);
        
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
                //  ОЧИСТКА 
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

        private async Task ProcessTcpCommandAsync(string json)
        {
            try
            {
                // "Подсматриваем" тип
                var baseMsg = JsonSerializer.Deserialize<TcpMessage>(json);

                // Выбираем, что делать
                switch (baseMsg.Type)
                {
                    case "PrivateMessage":
                        await HandlePrivateMessageAsync(json);
                        break;
                    case "GetPrivateHistory":
                        await HandlePrivateHistoryRequestAsync(json);
                        break;
                    default:
                        _logSystem($"TCP: Неизвестный тип сообщения от '{_userName}': {baseMsg.Type}");
                        break;
                }
            }
            catch (Exception ex)
            {
                _logSystem($"TCP: Ошибка парсинга JSON от '{_userName}': {ex.Message}");
            }
        }

        private async Task HandlePrivateHistoryRequestAsync(string json)
        {
            var req = JsonSerializer.Deserialize<PrivateHistoryRequest>(json);
            if (req == null) return;
            _logSystem($"TCP: Клиент '{_userName}' запросил историю c '{req.WithUser}'.");
            List<Message> history = _privateHistoryManager.GetHistory(_userName, req.WithUser);

            var response = new PrivateHistoryResponse
            {
                WithUser = req.WithUser,
                Messages = history
            };

            string jsonResponse = JsonSerializer.Serialize(response);
            byte[] buffer = Encoding.UTF8.GetBytes(jsonResponse);
            await _stream.WriteAsync(buffer, 0, buffer.Length);
        }

        private async Task HandlePrivateMessageAsync(string json)
        {
            //Парсим полное сообщение
            var pmRequest = JsonSerializer.Deserialize<PrivateMessageRequest>(json);
            if (pmRequest == null) return;

            _logSystem($"TCP: PM от '{_userName}' для '{pmRequest.ToUser}'");
            _privateHistoryManager.AddMessage(_userName, pmRequest.ToUser, pmRequest.Message);
            // Ищем получателя
            TcpClient recipientClient = _userManager.GetUser(pmRequest.ToUser);

            if (recipientClient != null && recipientClient.Connected)
            {
                var relayMsg = new PrivateMessageRelay
                {
                    FromUser = _userName,
                    Message = pmRequest.Message
                };

                // Сериализуем и отправляем
                string relayJson = JsonSerializer.Serialize(relayMsg);
                byte[] buffer = Encoding.UTF8.GetBytes(relayJson);

                await recipientClient.GetStream().WriteAsync(buffer, 0, buffer.Length);
            }
            else
            {
                _logSystem($"TCP: Получатель '{pmRequest.ToUser}' не найден или оффлайн.");
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