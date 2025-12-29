using System;
using System.Collections.Generic;
using System.Text;

namespace Models
{
    internal class ErrorViewModel
    {
        public string? RequestId { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}
