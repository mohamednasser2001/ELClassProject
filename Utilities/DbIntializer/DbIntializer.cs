using DataAccess;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Utilities.DbIntializer
{
    public class DbIntializer :IDbIntializer
    {
        private readonly ApplicationDbContext _context;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly UserManager<ApplicationUser> _userManager;

        public DbIntializer(ApplicationDbContext context, RoleManager<IdentityRole> roleManager, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _roleManager = roleManager;
            _userManager = userManager;
        }

        public void Initialize()
        {
            try
            {

                if (_context.Database.GetPendingMigrations().Any())
                {
                    _context.Database.Migrate();
                }

                if (!_roleManager.Roles.Any())
                {
                    _roleManager.CreateAsync(new("SuperAdmin")).GetAwaiter().GetResult();
                    _roleManager.CreateAsync(new("Admin")).GetAwaiter().GetResult();
                    _roleManager.CreateAsync(new("Teacher")).GetAwaiter().GetResult();
                    _roleManager.CreateAsync(new("Student")).GetAwaiter().GetResult();
                }

                var adminEmail = "Admin123@gmail.com";

                var adminUser = _userManager.FindByEmailAsync(adminEmail).GetAwaiter().GetResult();

                if (adminUser == null)
                {
                    adminUser = new ApplicationUser
                    {
                        Email = adminEmail,
                        UserName = "SuperAdmin123",
                        EmailConfirmed = true
                    };

                    var result = _userManager.CreateAsync(adminUser, "Admin123@gmail.com")
                                             .GetAwaiter().GetResult();

                    if (result.Succeeded)
                    {
                        _userManager.AddToRoleAsync(adminUser, "SuperAdmin")
                                    .GetAwaiter().GetResult();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{ex.Message}");
            }
        }
    }
}
