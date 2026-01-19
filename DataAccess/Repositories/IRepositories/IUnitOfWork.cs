using Microsoft.EntityFrameworkCore.Storage;
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
        IRepository<Lesson> LessonRepository { get; }
        IRepository<CHMessage> CHMessageRepository { get; }
        IRepository<Conversation> ConversationRepository { get; }
        IRepository<InstructorCourse> InstructorCourseRepository { get; }
        IRepository<InstructorStudent> InstructorStudentRepository { get; }
        IRepository<StudentCourse> StudentCourseRepository { get; }
        Task<IDbContextTransaction> BeginTransactionAsync();
        Task<bool> CommitAsync();
        void Dispose();
    }
}
