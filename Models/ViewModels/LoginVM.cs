using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Models.ViewModels
{
     public class LoginVM
    {
        [Required]
        public string EmailOrUserName { get; set; }
        [DataType(DataType.Password), Required]
        public string Password { get; set; }
        public bool RememberMe { get; set; }
    }
}
