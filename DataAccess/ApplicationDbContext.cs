using System.Reflection.Emit;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Models;

namespace DataAccess
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        private readonly IConfiguration _configuration;

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IConfiguration configuration)
        : base(options)
        {
            _configuration = configuration;
        }

        //public ApplicationDbContext(IConfiguration configuration)
        //{
        //    _configuration = configuration;
        //}

        public DbSet<Course> Courses { get; set; }
        public DbSet<Student> Students { get; set; }
        public DbSet<Instructor> Instructors { get; set; }
        public DbSet<Lesson> Lesson { get; set; }

        public DbSet<Conversation> Conversations { get; set; }
        public DbSet<CHMessage> CHMessages { get; set; }
        public DbSet<StudentCourse> StudentCourses { get; set; }
        public DbSet<InstructorCourse> InstructorCourses { get; set; }
        public DbSet<InstructorStudent> InstructorStudents { get; set; }
        public DbSet<ApplicationUser> ApplicationUsers { get; set; }
        public DbSet<ContactUs> ContactUs { get; set; }
        public DbSet<Appointment> Appointments { get; set; }
        public DbSet<StudentAppointment> StudentAppointments { get; set; }


        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            const int idMaxLength = 255;

            builder.Entity<ApplicationUser>(entity =>
            {
                entity.Property(u => u.Id).HasMaxLength(idMaxLength);
            });

            builder.Entity<Instructor>(entity =>
            {
                entity.Property(i => i.Id).HasMaxLength(idMaxLength);
                entity.Property(i => i.CreatedById).HasMaxLength(idMaxLength);

                entity.HasOne(i => i.ApplicationUser)
                    .WithOne(u => u.Instructor)
                    .HasForeignKey<Instructor>(i => i.Id);

                entity.HasOne(i => i.CreatedByUser)
                    .WithMany()
                    .HasForeignKey(i => i.CreatedById)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<Student>(entity =>
            {
                entity.Property(s => s.Id).HasMaxLength(idMaxLength);
            });

            builder.Entity<Course>(entity =>
            {
                entity.Property(c => c.CreatedById).HasMaxLength(idMaxLength);
                entity.HasOne(c => c.CreatedByUser)
                    .WithMany()
                    .HasForeignKey(c => c.CreatedById)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            builder.Entity<Lesson>(entity =>
            {
                entity.Property(l => l.CreatedById).HasMaxLength(idMaxLength);
                entity.HasOne(i => i.CreatedByUser)
                    .WithMany()
                    .HasForeignKey(i => i.CreatedById)
                    .OnDelete(DeleteBehavior.SetNull);
            });

            builder.Entity<Conversation>(entity =>
            {
                entity.Property(c => c.StudentId).HasMaxLength(idMaxLength);
                entity.Property(c => c.InstructorId).HasMaxLength(idMaxLength);

                entity.HasOne(c => c.Student)
                    .WithMany()
                    .HasForeignKey(c => c.StudentId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(c => c.Instructor)
                    .WithMany()
                    .HasForeignKey(c => c.InstructorId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<CHMessage>(entity =>
            {
                entity.Property(m => m.SenderId).HasMaxLength(idMaxLength);
                entity.Property(m => m.ReceiverId).HasMaxLength(idMaxLength);

                entity.HasOne(m => m.Conversation)
                    .WithMany(c => c.CHMessages)
                    .HasForeignKey(m => m.ConversationId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<InstructorStudent>(entity =>
            {
                entity.HasKey(e => new { e.InstructorId, e.StudentId });
                entity.Property(e => e.InstructorId).HasMaxLength(idMaxLength);
                entity.Property(e => e.StudentId).HasMaxLength(idMaxLength);

                entity.HasOne(e => e.Instructor)
                    .WithMany(i => i.InstructorStudents) 
                    .HasForeignKey(e => e.InstructorId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Student)
                    .WithMany(s => s.InstructorStudents) 
                    .HasForeignKey(e => e.StudentId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<StudentCourse>(entity =>
            {
                entity.HasKey(sc => new { sc.StudentId, sc.CourseId });
                entity.Property(sc => sc.StudentId).HasMaxLength(idMaxLength);
            });

            builder.Entity<InstructorCourse>(entity =>
            {
                entity.HasKey(ic => new { ic.InstructorId, ic.CourseId });
                entity.Property(ic => ic.InstructorId).HasMaxLength(idMaxLength);
            });

            builder.Entity<Appointment>()
            .HasOne(a => a.Instructor)
            .WithMany(i => i.Appointments)
            .HasForeignKey(a => a.InstructorId)
            .OnDelete(DeleteBehavior.NoAction);


            builder.Entity<StudentAppointment>()
                .HasOne(sa => sa.Appointment)
                .WithMany()
                .HasForeignKey(sa => sa.AppointmentId)
                .OnDelete(DeleteBehavior.ClientSetNull);


            builder.Entity<StudentAppointment>()
                .HasOne(sa => sa.Appointment)
                .WithMany(a => a.StudentAppointments)
                .HasForeignKey(sa => sa.AppointmentId);
        }


    }
}
