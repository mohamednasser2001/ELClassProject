using DataAccess.Repositories.IRepositories;
using Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace DataAccess.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        public UnitOfWork(IRepository<Course> courseRepository, IRepository<Instructor> instructorRepository,
           IRepository<Student> studentRepository)
        {
            CourseRepository = courseRepository;
            InstructorRepository = instructorRepository;
            StudentRepository = studentRepository;
        }
        public IRepository<Course> CourseRepository { get; }
        public IRepository<Instructor> InstructorRepository { get; }
        public IRepository<Student> StudentRepository { get; }
    }
}
