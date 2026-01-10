using System.Threading.Tasks;
using DataAccess;
using DataAccess.Repositories;
using DataAccess.Repositories.IRepositories;
using ELClass.Hubs; 
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Models;
using Models.Utilites;
using Scalar.AspNetCore;
using Utilities.DbIntializer;

namespace ELClass
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

           
            builder.Services.AddControllersWithViews();

           
            builder.Services.AddSignalR();

            
            builder.Services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

           
            builder.Services.AddIdentity<ApplicationUser, IdentityRole>(config =>
            {
                config.Password.RequiredLength = 8;
                config.User.RequireUniqueEmail = true;
                config.SignIn.RequireConfirmedEmail = false;
                config.Lockout.AllowedForNewUsers = true;
                config.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                config.Lockout.MaxFailedAccessAttempts = 5;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

            builder.Services.ConfigureApplicationCookie(options =>
            {
                options.LoginPath = "/Identity/Account/Login";
                options.AccessDeniedPath = "/Identity/Account/AccessDenied";
            });

            builder.Services.AddTransient<IEmailSender, EmailSender>();

            builder.Services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(30);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });

            
            builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
            builder.Services.AddScoped<IRepository<Course>, Repository<Course>>();
            builder.Services.AddScoped<IRepository<Instructor>, Repository<Instructor>>();
            builder.Services.AddScoped<IRepository<Student>, Repository<Student>>();
            builder.Services.AddScoped<IRepository<InstructorCourse>, Repository<InstructorCourse>>();
            builder.Services.AddScoped<IRepository<InstructorStudent>, Repository<InstructorStudent>>();
            builder.Services.AddScoped<IRepository<StudentCourse>, Repository<StudentCourse>>();
            builder.Services.AddScoped<IRepository<Lesson>, Repository<Lesson>>();
            builder.Services.AddScoped<IRepository<ChatMessage>, Repository<ChatMessage>>();
            builder.Services.AddScoped<IDbIntializer, DbIntializer>();

            var app = builder.Build();

           
            if (app.Environment.IsDevelopment())
            {
                app.MapScalarApiReference();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();

            app.UseSession();

            
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapStaticAssets();

          
            app.MapHub<ChatHub>("/chatHub");

            
            app.MapControllerRoute(
                 name: "default",
                 pattern: "{area=Admin}/{controller=Home}/{action=Index}/{id?}")
                 .WithStaticAssets();

            
            using (var scope = app.Services.CreateScope())
            {
                

                var dbInitializer = scope.ServiceProvider.GetRequiredService<IDbIntializer>();
                dbInitializer.Initialize();
            }

            app.Run();
        }
    }
}