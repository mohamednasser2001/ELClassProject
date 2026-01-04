using System.Threading.Tasks;
using DataAccess;
using DataAccess.Repositories;
using DataAccess.Repositories.IRepositories;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Models;
using Models.Utilites;
using Scalar;
using Scalar.AspNetCore;
namespace ELClass
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddControllersWithViews();

            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                  options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            builder.Services.AddIdentity<ApplicationUser, IdentityRole>(config =>
            {
                config.Password.RequiredLength = 8;
                config.User.RequireUniqueEmail = true;
                config.SignIn.RequireConfirmedEmail=true;
                

                // ??????? ??? ?????? (Lockout)
                config.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                config.Lockout.MaxFailedAccessAttempts = 5;
                config.Lockout.AllowedForNewUsers = true;
            })
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();
            builder.Services.AddTransient<IEmailSender, EmailSender>();

            // add the DbContext 

            builder.Services.AddDbContext<ApplicationDbContext>(option =>
            {
                option.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
            });

            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(30);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });

            // add services to the container
            builder.Services.AddScoped<IUnitOfWork,UnitOfWork>();
            builder.Services.AddScoped<IRepository<Course>,Repository<Course>>();
            builder.Services.AddScoped<IRepository<Instructor>,Repository<Instructor>>();
            builder.Services.AddScoped<IRepository<Student>,Repository<Student>>();
            builder.Services.AddScoped<IRepository<InstructorCourse>,Repository<InstructorCourse>>();
            builder.Services.AddScoped<IRepository<InstructorStudent>,Repository<InstructorStudent>>();
            builder.Services.AddScoped<IRepository<StudentCourse>,Repository<StudentCourse>>();
            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment() || app.Environment.IsProduction())
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
                app.MapScalarApiReference();

            }
            app.UseSession();
            app.UseStaticFiles();
            app.UseHttpsRedirection();
            app.UseRouting();

            app.UseAuthorization();

            app.MapStaticAssets();
            app.MapControllerRoute(
                name: "default",
                pattern: "{area=Admin}/{controller=Home}/{action=Index}/{id?}")
                .WithStaticAssets();

            using (var scope = app.Services.CreateScope())
            {
                var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

                string[] roleNames = { "Admin", "Teacher", "Student" };

                foreach (var roleName in roleNames)
                {
                    // إذا كان الدور غير موجود، قم بإنشائه
                 
                    if (!await roleManager.RoleExistsAsync(roleName))
                    {
                        await roleManager.CreateAsync(new IdentityRole(roleName));
                    }
                }
            }

            app.Run();
        }
    }
}
