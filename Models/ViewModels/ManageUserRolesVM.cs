using System;
using System.Collections.Generic;
using System.Text;

namespace Models.ViewModels
{
    public class ManageUserRolesVM
    {
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string SelectedRole { get; set; } = string.Empty;
    }
}
