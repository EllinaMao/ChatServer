using Logic.MessagesFiles;

namespace Logic
{
    public class ChatHistoryManager
    {
        private readonly List<Message> messages = new List<Message>();
        private readonly object _lock = new object();
        private readonly int _maxHistorySize = 100;//well это учебный проэкт

        public void AddMessage(Message message)
        {
            lock (_lock)
            {
                messages.Add(message);
                if (messages.Count > _maxHistorySize)
                {
                    messages.RemoveAt(0);
                }
            }
        }
        public List<Message> GetHistory()
        {
            lock (_lock)
            {
                return new List<Message>(messages);
            }
        }
    }
}
