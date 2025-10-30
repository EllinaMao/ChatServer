namespace Logic.MessagesFiles
{
    internal class ErrorMessage : TcpMessage
    {
        public ErrorMessage() { Type = "Error"; }
        public string Reason { get; set; } // Причина (н-р, "Имя занято")    }
    }
}