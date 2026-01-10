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
using Utilities.DbIntializer;
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
                config.SignIn.RequireConfirmedEmail = false;
                config.Lockout.AllowedForNewUsers = true;


                // ??????? ??? ?????? (Lockout)
                config.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                config.Lockout.MaxFailedAccessAttempts = 5;
                config.Lockout.AllowedForNewUsers = true;
            })
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();
            // add identity to url 
            builder.Services.ConfigureApplicationCookie(options =>
            {
                options.LoginPath = "/Identity/Account/Login";
                options.AccessDeniedPath = "/Identity/Account/AccessDenied";
            });
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
            builder.Services.AddScoped<IRepository<StudentCourse>, Repository<StudentCourse>>();
            builder.Services.AddScoped<IDbIntializer, DbIntializer>();
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

            // Db Intializer

            using (var scope = app.Services.CreateScope())
            {
                var dbInitializer = scope.ServiceProvider.GetRequiredService<IDbIntializer>();
                dbInitializer.Initialize();
            }


            app.Run();
        }
    }
}
