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

        public Action<string>? MessageGetEvent; // сообщение участника
        public Action<string>? ServerLogEvent;  // cообщение сервера

        // --- Менеджеры (Состояние) ---
        private readonly UsersManager _userManager = new UsersManager();
        private readonly ChatHistoryManager _historyManager = new ChatHistoryManager();

        // --- Сервисы (Поведение) ---
        private readonly UdpService _udpService;
        private readonly TcpService _tcpService;

        private bool _isServerRunning = false;


        public ServerController(SynchronizationContext ctx = null)
        {

            if (ctx != null)
            {
                this.ctx = ctx;
            }
            // Создаем сервисы, передавая им зависимости
            // (менеджеры и методы логирования)
            _udpService = new UdpService(_historyManager, LogMessages, LogSystem);
            _tcpService = new TcpService(_userManager, _historyManager, LogSystem);
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
            _isServerRunning = true;
            _ = Task.Run(() => _udpService.StartListeningAsync());
            _ = Task.Run(() => _tcpService.StartListeningAsync());

            LogSystem("Server started");
        }

        public void Stop()
        {
            if (!_isServerRunning) return;

            LogSystem("Server stopping...");
            _isServerRunning = false;
            // 1. Останавливаем "слушателей" (новые подключения)
            _tcpService.Stop();
            _udpService.Stop();

            // 2. Выбрасываем всех, кто уже подключен
            _userManager.CloseAllConnections();
            LogSystem("Server stopped");
        }

    }
}
