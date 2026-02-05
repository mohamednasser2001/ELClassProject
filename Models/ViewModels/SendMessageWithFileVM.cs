using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Models.ViewModels
{
    public class SendMessageWithFileVM
    {
        [Required]
        public string ReceiverId { get; set; } = null!;

        public string? Content { get; set; }

        public IFormFile? File { get; set; }
    }
}
