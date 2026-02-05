using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Models
{
    public class CHMessage
    {
        public long Id { get; set; }

        [Required]
        public int ConversationId { get; set; }

        [ForeignKey(nameof(ConversationId))]
        public Conversation Conversation { get; set; } = null!;

        [Required]
        [StringLength(450)]
        public string SenderId { get; set; } = null!;

        [Required]
        [StringLength(450)]
        public string ReceiverId { get; set; } = null!;

        
        [MaxLength(2000)]
        public string? Content { get; set; } 

        [StringLength(260)]
        public string? AttachmentOriginalName { get; set; }  

        [StringLength(260)]
        public string? AttachmentStoredName { get; set; }    

        [StringLength(100)]
        public string? AttachmentContentType { get; set; }   

        public long? AttachmentSize { get; set; }

        [Required]
        public DateTime SentAt { get; set; } = DateTime.UtcNow;

        [Required]
        public bool IsRead { get; set; }

        public DateTime? ReadAt { get; set; }
    }
}
