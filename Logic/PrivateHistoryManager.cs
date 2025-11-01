using Logic.MessagesFiles;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logic
{
    public class PrivateHistoryManager
    {
        private readonly ConcurrentDictionary<string, List<Message>> _histories
            = new ConcurrentDictionary<string, List<Message>>();

        private const int MAX_HISTORY_LINES = 100;

        // Помощник для создания ключа
        private string GetKeyPair(string userA, string userB)
        {
            return string.Compare(userA, userB) < 0
                ? $"{userA}_{userB}"
                : $"{userB}_{userA}";
        }

        // Сохраняем сообщение
        public void AddMessage(string fromUser, string toUser, string msgContent)
        {
            string key = GetKeyPair(fromUser, toUser);
            var newMessage = new Message { Name = fromUser, Msg = msgContent };

            _histories.AddOrUpdate(key,
                (k) => new List<Message> { newMessage }, // Создаем, если нет
                (k, existingList) => // Обновляем, если есть
                {
                    lock (existingList) // Блокируем список
                    {
                        existingList.Add(newMessage);
                        if (existingList.Count > MAX_HISTORY_LINES)
                        {
                            existingList.RemoveAt(0);
                        }
                    }
                    return existingList;
                }
            );
        }

        // Отдаем историю
        public List<Message> GetHistory(string userA, string userB)
        {
            string key = GetKeyPair(userA, userB);

            if (_histories.TryGetValue(key, out var list))
            {
                lock (list)
                {
                    return new List<Message>(list); // Возвращаем КОПИЮ
                }
            }
            return new List<Message>(); // Пустой список
        }
    }
}
