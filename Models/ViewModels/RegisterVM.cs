using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Models.ViewModels
{
    public class RegisterVM
    {

        [RegularExpression(@"^[A-Za-z\u0600-\u06FF0-9\s]+$",
        ErrorMessage = "Name can contain Arabic letters, English letters, and numbers only")]
        [Display(Name = "Name")]
        public string? NameEN { get; set; }

        [RegularExpression(@"^[A-Za-z\u0600-\u06FF0-9\s]+$",
            ErrorMessage = "يسمح بالحروف العربية والإنجليزية والأرقام فقط")]
        [Display(Name = "الاسم")]
        public string? NameAR { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid Email Address")]
        public string Email { get; set; } = null!;

        [Required(ErrorMessage = "Password is required")]
        [DataType(DataType.Password)]
        [StringLength(32, MinimumLength = 8)]
        public string Password { get; set; } = null!;

        [Required(ErrorMessage = "Confirm Password is required")]
        [Compare(nameof(Password))]
        [DataType(DataType.Password)]
        public string ConfirmPassword { get; set; } = null!;
    }
}
