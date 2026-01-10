using System;
using System.Collections.Generic;
using System.Text;

namespace Models
{
    public class ChatMessage
    {
        public int Id { get; set; }
        public string SenderId { get; set; } // الطالب أو المدرس
        public string ReceiverId { get; set; } // المستلم
        public string Message { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;


        public bool IsRead { get; set; } = false;
    }
}
