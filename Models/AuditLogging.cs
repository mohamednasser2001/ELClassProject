using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Models
{
    public class AuditLogging
    {
       
        public DateTime? CreatedAt { get; set; } = DateTime.Now;
        public string? CreatedById { get; set; } = null;
        [ForeignKey(nameof(CreatedById))]
        public ApplicationUser? CreatedByUser { get; set; }

        public DateTime? UpdatedAt { get; set; }
    }
}
