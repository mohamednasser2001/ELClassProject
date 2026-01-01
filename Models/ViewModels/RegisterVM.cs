using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Models.ViewModels
{
    public class RegisterVM
    {
        // [RegularExpression("[A-Za-z]*$", ErrorMessage = "English Char is only Allowed")]
        [Required(ErrorMessage = "Name is required")]
        [RegularExpression(
        @"^([\u0600-\u06FF\s]+|[A-Za-z\s]+)$",
        ErrorMessage = "Name must be Arabic or English only"
          )]
        public string Name { get; set; }
        //[RegularExpression(@"^[\u0600-\u06FF\s]+$", ErrorMessage = "Arabic letters only")]
        //public string NameAR { get; set; }
        [EmailAddress]
        public string Email { get; set; }
        [DataType(DataType.Password), Length(8, 32)]
        public string Password { get; set; }
        [Compare(nameof(Password)), DataType(DataType.Password), Length(8, 32)]
        public string ConfirmPassword { get; set; }
    }
}
