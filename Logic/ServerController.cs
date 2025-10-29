using Logic.MessagesFiles;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Logic
{
    public class ServerController
    {
        /*TODO
        Сервер : 
        Сервер должен выполнять несколько задач одновременно (используя async/await и Task):

        Слушать UDP Multicast: Он должен быть таким же участником мультикаст-группы, как и клиенты. Его цель — слушать общий чат и сохранять историю.

        Слушать TCP (TcpListener): Он должен принимать новые TCP-подключения от клиентов.

        Обрабатывать TCP-клиентов: Для каждого подключенного клиента сервер должен в отдельном Task асинхронно читать данные из его NetworkStream.

        Хранить состояние: У сервера должен быть список участников
         */


        private SynchronizationContext ctx;


        public Action<string>? MessageGetEvent;//сообщение участника
        public Action<string>? ServerLogEvent;//cообщение сервера

        private UdpClient _udpListener; //для публичного соединения и общего чата
        private TcpListener _tcpListener; //logs or private chat, still deciding

        private readonly UsersManager _userManager = new UsersManager();//cписок пользователей
        private readonly ChatHistoryManager _historyManager = new ChatHistoryManager();
        //история чатов

        private const int UDP_PORT = 8001;
        private const int TCP_PORT = 8002;

        private readonly IPAddress MULTICAST_GROUP = IPAddress.Parse("239.0.0.1");


        private bool _isServerRunning = false; // <--- Флаг для управления циклом

        public ServerController(SynchronizationContext ctx = null)
        {

            if (ctx != null)
            {
                this.ctx = ctx;
            }


            // Настраиваем UDP на прослушивание и что бы смотреть тут чат
            // 1. Создаем UdpClient
            _udpListener = new UdpClient();
            _tcpListener = new TcpListener(IPAddress.Any, TCP_PORT);
            // 2.Разрешаем другим (и нам) слушать этот же порт
            _udpListener.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            // 3. Занимаем (Bind) порт
            _udpListener.Client.Bind(new IPEndPoint(IPAddress.Any, UDP_PORT));
            // 4. Присоединяемся к мультикаст-группе
            _udpListener.JoinMulticastGroup(MULTICAST_GROUP, IPAddress.Any);
        }


        public void LogSystem(string msg)
        {
            if (ctx != null)

                ctx.Post(d => ServerLogEvent?.Invoke(msg), null);
            else
                ServerLogEvent?.Invoke(msg);
        }

        public void LogMessages(string name, string msg)
        {
            string fullMsg = $"{name}: {msg}";

            if (ctx != null)

                ctx.Post(d => MessageGetEvent?.Invoke(fullMsg), null);
            else
                MessageGetEvent?.Invoke(fullMsg);
        }

        public void Start()
        {
            LogSystem("Server started");
            _ = Task.Run(ListenUdpAsync); // Асинхронный запуск UDP
            _ = Task.Run(ListenTCPAsync);
            _isServerRunning = true;

        }

        private async Task ListenTCPAsync()
        {
            try
            {
                _tcpListener.Start();
                LogSystem($"TCPLogs: listenig");
                while (_isServerRunning)
                {
                    TcpClient tcpClient = await _tcpListener.AcceptTcpClientAsync();
                    LogSystem($"TCP: Новый клиент {tcpClient.Client.RemoteEndPoint} подключился.");
                    _ = Task.Run(() => HandleClientAscync(tcpClient));
                }
            }
            catch (SocketException ex) when (ex.SocketErrorCode == SocketError.Interrupted)
            {
                // Ожидаемое исключение при вызове _tcpListener.Stop()
                LogSystem("TCP Listener stopped.");
            }
            catch (Exception ex)
            {
                LogSystem($"Critical error for TCP Listener: {ex.Message}");
            }
        }
        //для одного клиента
        private async Task HandleClientAscync(TcpClient tcpUser)
        {
            NetworkStream stream = null;
            try
            {
                stream = tcpUser.GetStream();
                var clientEndpoint = tcpUser?.Client.RemoteEndPoint?.ToString();
                string? userName = null;

                byte[] buffer = new byte[1024];
                int byteRead = await stream.ReadAsync(buffer, 0, buffer.Length);


                string jsonRequest = Encoding.UTF8.GetString(buffer, 0, buffer.Length);
                var connectMsg = CommandMessage.FromJson(jsonRequest);
                userName = connectMsg.Name;

                _userManager.AddUser(userName, tcpUser);
                LogSystem($"TCP: Клиент {clientEndpoint} зарегистрирован как '{userName}'.");
                // TODO: Отправить новому клиенту историю чата (из _historyManager)
                // TODO: Оповестить всех (по UDP или TCP) о новом пользователе????????????
                while (tcpUser.Connected && _isServerRunning)
                {
                    byteRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                    if (byteRead == 0)

                        // Клиент отключился
                        break;
                }

                string clientJson = Encoding.UTF8.GetString(buffer, 0, byteRead);
                // TODO: Обработать команду от клиента (например, приватное сообщение)
                // Например: { "Type": "PrivateMessage", "To": "Petya", "Msg": "Привет!" }
                // 1. Парсим JSON
                // 2. Находим TcpClient "Petya" в _clientManager
                // 3. Отправляем сообщение "Petya" в его NetworkStream
            }

            catch (IOException ex)
            {
                LogSystem($"TCP: Client maybe disconnected (IOException): {ex.Message}");
            }
            catch (Exception ex)
            {
                LogSystem($"TCP: Error in handling client: {ex.Message}");
            }
            finally
            {
                // TODO: Удалить клиента из _clientManager
                // _clientManager.RemoveUser(userName);
                stream?.Close();
                tcpUser?.Close();
                LogSystem($"TCP: Client {tcpUser?.Client?.RemoteEndPoint} disconnected.");
            }
        }



        private async Task ListenUdpAsync()
        {
            LogSystem($"Udp: Listening users chat");
            while (true)
            {
                try
                {
                    var result = await _udpListener.ReceiveAsync();
                    var json = Encoding.UTF8.GetString(result.Buffer);
                    var message = Logic.MessagesFiles.Message.FromJson(json);

                    _historyManager.AddMessage(message);
                    LogMessages(message.Name, message.Msg);

                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка UDP: {ex.Message}");
                }
            }
        }
        // Внутри класса ServerController
        private async Task SendAllUsersListAsync()
        {
            // 1. Получаем актуальный список имен из менеджера
            List<string> userNames = _userManager.GetAllUserNames();

            // 2. Создаем сообщение
            var userListMsg = new UserListMessage
            {
                Users = userNames
            };

            // 3. Сериализуем его (предполагая, что у вас есть Message.ToJson() 
            //    или используйте JsonSerializer)
            //    Убедитесь, что ваш метод ToJson() может работать с TcpMessage
            string json = JsonSerializer.Serialize(userListMsg); // Используем System.Text.Json
            byte[] buffer = Encoding.UTF8.GetBytes(json);

            LogSystem($"SYSTEM: Список обновлен. {userNames.Count} пользователей онлайн.");

            // 4. Используем метод ClientManager для отправки всем
            await _userManager.SendMessageAsync(buffer);
        }


    }
}
