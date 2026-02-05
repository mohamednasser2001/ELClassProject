using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Http;

namespace Models.ViewModels
{
    public class ChatSendVM
    {
        public long Id { get; set; }
        public string SenderId { get; set; } = null!;
        public string ReceiverId { get; set; } = null!;
        public string? Content { get; set; }
        public DateTime SentAt { get; set; }
        public bool IsRead { get; set; }

        // attachment
        public string? AttachmentOriginalName { get; set; }
        public string? AttachmentContentType { get; set; }
        public long? AttachmentSize { get; set; }

        // ده مهم جدًا: هنرجعه جاهز عشان الـ JS يرسمه فورًا (history أو live)
        public string? AttachmentUrl { get; set; }
        public bool HasAttachment => !string.IsNullOrWhiteSpace(AttachmentUrl);
        public bool IsImage => AttachmentContentType?.StartsWith("image/") == true;
    }
}
