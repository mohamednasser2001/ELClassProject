using Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace DataAccess.Repositories.IRepositories
{
    public interface IUnitOfWork
    {
        IRepository<Course> CourseRepository { get; }
        IRepository<Instructor> InstructorRepository { get; }
        IRepository<Student> StudentRepository { get; }
        Task<bool> CommitAsync();
        void Dispose();
    }
}
