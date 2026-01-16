using System;
using System.Collections.Generic;
using System.Text;

namespace Models
{
    public class ChatMessage
    {
        public int Id { get; set; }
        public string SenderId { get; set; } = string.Empty;
        public string ReceiverId { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.Now;


        public bool IsRead { get; set; } = false;
    }
}
