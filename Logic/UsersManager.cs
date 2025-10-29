using System.Collections.Concurrent;
using System.Net.Sockets;
using Logic.MessagesFiles;

namespace Logic
{
    public class UsersManager
    {
        private readonly ConcurrentDictionary<string, TcpClient> _users = new ConcurrentDictionary<string, TcpClient>();
        /// <summary>
        /// Попытка добавить нового клиента в список.
        /// </summary>
        /// <param name="userName">Имя пользователя (ключ)</param>
        /// <param name="user">Объект TcpClient (значение)</param>
        /// <returns>True, если клиент успешно добавлен. False, если имя уже занято.</returns>

        public bool AddUser(string username, TcpClient user)//держать открытую и неактивную связь дешевле чем открывать и закрывать
        {
            return _users.TryAdd(username, user);
        }

        /// <summary>
        /// Удаляет клиента из списка по имени пользователя.
        /// </summary>
        /// <param name="userName">Имя пользователя для удаления, но не думаю что это нужно</param>
        /// 
        public bool RemoveUser(string username)
        {
            return _users.TryRemove(username, out _);
        }

        /// <summary>
        /// Получает TcpClient по имени пользователя.
        /// Нужно для отправки приватных сообщений.
        /// </summary>
        /// <param name="userName">Имя пользователя-получателя.</param>
        /// <returns>TcpClient или null, если пользователь не найден.</returns>
        /// 
        public TcpClient? GetUser(string username)
        {
            _users.TryGetValue(username, out TcpClient user);
            return user;//null or user
        }
        /// <summary>
        ///for listbox
        /// Получает список имен всех, кто сейчас онлайн.
        /// </summary>
        /// <returns>Список имен (string).</returns>
        public List<string> GetAllUserNames()
        {
            // .Keys - это потокобезопасная коллекция всех ключей (имен)
            return _users.Keys.ToList();
        }

        /// <summary>
        /// (Опционально) Отправляет TCP-сообщение всем подключенным клиентам.
        /// Полезно для системных оповещений (например, "Вася вошел в чат").
        /// </summary>
        /// 
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
                    RemoveUser(pair.Key);//а зачем кст?
                }
            }
        }

  
    }

}