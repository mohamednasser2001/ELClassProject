using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Models
{
    internal class AuditLogging
    {
       
        public DateTime? CreateAT { get; set; } = DateTime.Now;
        public string? CreateById { get; set; }
        [ForeignKey(nameof(CreateById))]
        public ApplicationUser? ApplicationUser { get; set; }
       
        public DateTime? UpdatedAT { get; set; }
    }
}
