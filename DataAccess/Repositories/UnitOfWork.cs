using DataAccess.Repositories.IRepositories;
using Microsoft.EntityFrameworkCore;
using Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext context;

        public UnitOfWork(IRepository<Course> courseRepository, IRepository<Instructor> instructorRepository,
           IRepository<Student> studentRepository , ApplicationDbContext context , IRepository<InstructorCourse> instructorCourseRepository ,
           IRepository<InstructorStudent> instructorStudentRepository , IRepository<StudentCourse> studentCourseRepository)
        {
            CourseRepository = courseRepository;
            InstructorRepository = instructorRepository;
            StudentRepository = studentRepository;
            this.context = context;
            InstructorCourseRepository = instructorCourseRepository;
            InstructorStudentRepository = instructorStudentRepository;
            StudentCourseRepository = studentCourseRepository;
        }
        public IRepository<Course> CourseRepository { get; }
        public IRepository<Instructor> InstructorRepository { get; }
        public IRepository<Student> StudentRepository { get; }
        public IRepository<InstructorCourse> InstructorCourseRepository { get; }
        public IRepository<InstructorStudent> InstructorStudentRepository { get; }
        public IRepository<StudentCourse> StudentCourseRepository { get; }
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
        public void Dispose()
        {
            context.Dispose();
        }
    }
}
