namespace Logic.MessagesFiles
{
    internal class HistoryMessage:TcpMessage
    {
        public HistoryMessage() { Type = "HistoryMessage"; }
        public List<Message> Messages { get; set; }
    }
}