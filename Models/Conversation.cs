using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace Models
{
   [Index(nameof(StudentId), nameof(InstructorId), IsUnique = true)]
    public class Conversation
    {
        public int Id { get; set; }

        [Required]
        [StringLength(450)]
        public string StudentId { get; set; } = null!;

        [Required]
        [StringLength(450)]
        public string InstructorId { get; set; } = null!;

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Required]
        public DateTime LastMessageAt { get; set; } = DateTime.UtcNow;

        [StringLength(300)]
        public string? LastMessagePreview { get; set; }

        [StringLength(450)]
        public string? LastMessageSenderId { get; set; }

        [Required]
        public int UnreadForStudent { get; set; }

        [Required]
        public int UnreadForInstructor { get; set; }
      
        public Student Student { get; set; } = null!;

     
        public Instructor Instructor { get; set; } = null!;

        public ICollection<CHMessage>  CHMessages { get; set; } = new List<CHMessage>();
    }
}
