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
           IRepository<Student> studentRepository , ApplicationDbContext context)
        {
            CourseRepository = courseRepository;
            InstructorRepository = instructorRepository;
            StudentRepository = studentRepository;
            this.context = context;
        }
        public IRepository<Course> CourseRepository { get; }
        public IRepository<Instructor> InstructorRepository { get; }
        public IRepository<Student> StudentRepository { get; }
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
