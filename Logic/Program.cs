namespace Logic
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var server = new ServerController();

            // 2. Подписываемся на события сервера, чтобы выводить их в консоль.

            // Событие для системных логов (старт, ошибки и т.д.)
            server.ServerLogEvent += (msg) =>
            {
                Console.ForegroundColor = ConsoleColor.Yellow; // Сделаем логи желтыми
                Console.WriteLine($"[SYSTEM] {msg}");
                Console.ResetColor();
            };

            // Событие для сообщений чата
            server.MessageGetEvent += (fullMsg) =>
            {
                Console.WriteLine(fullMsg); // Просто выводим "Имя: текст"
            };
            // 3. Запускаем сервер
            server.Start();

            // 4. Ждем, пока пользователь не нажмет Enter, чтобы консоль не закрылась.
            //    Метод Start() асинхронный (Task.Run), поэтому Main()
            //    продолжит выполнение. Console.ReadLine() блокирует
            //    основной поток и не дает программе завершиться.
            Console.WriteLine("Сервер запущен. Нажмите [Enter] для остановки.");
            Console.ReadLine();
        }
    }
}
