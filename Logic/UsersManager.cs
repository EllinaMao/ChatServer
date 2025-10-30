using System.Collections.Concurrent;
using System.Net.Sockets;
using Logic.MessagesFiles;

namespace Logic
{
    public class UsersManager
    {
        private readonly ConcurrentDictionary<string, TcpClient> _users = new ConcurrentDictionary<string, TcpClient>();
        /// Попытка добавить нового клиента в список.
        public bool AddUser(string username, TcpClient user)//держать открытую и неактивную связь дешевле чем открывать и закрывать
        {
            return _users.TryAdd(username, user);
        }

        /// Удаляет клиента из списка по имени пользователя.
        public bool RemoveUser(string username)
        {
            return _users.TryRemove(username, out _);
        }


        /// Получает TcpClient по имени пользователя.
        /// Нужно для отправки приватных сообщений.
        public TcpClient? GetUser(string username)
        {
            _users.TryGetValue(username, out TcpClient user);
            return user;//null or user
        }
        ///for listbox
        /// Получает список имен всех, кто сейчас онлайн.

        public List<string> GetAllUserNames()
        {
            // .Keys - это потокобезопасная коллекция всех ключей (имен)
            return _users.Keys.ToList();
        }

        /// Отправляет TCP-сообщение всем подключенным клиентам.
        /// Полезно для системных оповещений (например, "Вася вошел в чат").
        public async Task SendMessageAsync(byte[] message, string senderName = null)
        {
            foreach (var pair in _users)
            {
                // (Опция) Не отправлять сообщение обратно самому себе
                if (pair.Key == senderName)
                {
                    continue;
                }

                TcpClient client = pair.Value;
                try
                {
                    if (client.Connected)
                    {
                        // Получаем NetworkStream и асинхронно пишем в него
                        NetworkStream stream = client.GetStream();
                        await stream.WriteAsync(message, 0, message.Length);
                    }
                }
                catch (System.IO.IOException)
                {
                    RemoveUser(pair.Key);
                }
                catch (System.Exception)
                {
                    RemoveUser(pair.Key);
                }
            }
        }

        /// Принудительно закрывает все активные TCP-соединения
        /// и очищает список пользователей.
        /// Вызывается при остановке сервера.

        public void CloseAllConnections()
        {
            // ConcurrentDictionary безопасен для перебора,
            // даже если сессия попытается сама себя удалить
            foreach (var pair in _users)
            {
                try
                {
                    // 1. Закрываем сокет.
                    // Это немедленно вызовет IOException или ObjectDisposedException
                    // в задаче ClientSession.RunAsync(), которая слушает этот client.
                    pair.Value.Close();
                }
                catch (Exception)
                {
                    // Игнорируем ошибки при остановке.
                    // Клиент мог уже и сам отвалиться.
                }
            }

            // 2. Очищаем словарь.
            // Блок 'finally' в ClientSession.RunAsync() тоже попытается
            // вызвать RemoveUser, но это не страшно - он просто
            // не найдет ключ и ничего не сделает.
            _users.Clear();
        }

    }

}