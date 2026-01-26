using DataAccess.Repositories.IRepositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly IRepository<Conversation> conversationRepository;
        private readonly ApplicationDbContext context;

        public UnitOfWork(IRepository<Course> courseRepository, IRepository<Instructor> instructorRepository,
           IRepository<Student> studentRepository, IRepository<Lesson> lessonRepository, IRepository<CHMessage> ChatMessageRepositories
           , IRepository<Conversation> conversationRepository, ApplicationDbContext context, IRepository<InstructorCourse> instructorCourseRepository,
           IRepository<InstructorStudent> instructorStudentRepository, IRepository<StudentCourse> studentCourseRepository,
           IRepository<ContactUs> contactUsRepository , IRepository<Appointment> Appoinments , 
           IRepository<StudentAppointment> studentAppointmentRepository
           )
           {
            CourseRepository = courseRepository;
            InstructorRepository = instructorRepository;
            StudentRepository = studentRepository;
            LessonRepository = lessonRepository;
            ConversationRepository = conversationRepository;
            CHMessageRepository = ChatMessageRepositories;
            this.context = context;
            InstructorCourseRepository = instructorCourseRepository;
            InstructorStudentRepository = instructorStudentRepository;
            StudentCourseRepository = studentCourseRepository;
            ContactUsRepository = contactUsRepository;
            AppoinmentRepository = Appoinments;
            StudentAppointmentRepository = studentAppointmentRepository;
        }
        public IRepository<Course> CourseRepository { get; }
        public IRepository<Instructor> InstructorRepository { get; }
        public IRepository<Student> StudentRepository { get; }
         public IRepository<Lesson> LessonRepository { get; }
        public IRepository<CHMessage> CHMessageRepository { get; }
        public IRepository<Conversation> ConversationRepository { get; }
        public IRepository<InstructorCourse> InstructorCourseRepository { get; }
        public IRepository<InstructorStudent> InstructorStudentRepository { get; }
        public IRepository<StudentCourse> StudentCourseRepository { get; }
        public IRepository<ContactUs> ContactUsRepository { get; }
        public IRepository<Appointment> AppoinmentRepository { get; }
        public IRepository<StudentAppointment> StudentAppointmentRepository { get; }

        public async Task<bool> CommitAsync()
        {
            try
            {
                await context.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");

                return false;
            }
        }
        public async Task<IDbContextTransaction> BeginTransactionAsync()
        {
            return await context.Database.BeginTransactionAsync();
        }
        public void Dispose()
        {
            context.Dispose();
        }
    }
}
