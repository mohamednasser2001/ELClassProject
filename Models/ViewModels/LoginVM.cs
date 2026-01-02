using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Models.ViewModels
{
     public class LoginVM
    {
        [Required(ErrorMessage = "This field is required")]
        [Display(Name = "Email or User Name")]
        public string EmailOrUserName { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Display(Name = "Remember Me")]
        public bool RememberMe { get; set; }
    }
}
