using DataAccess.Repositories.IRepositories;
using Microsoft.EntityFrameworkCore;
using Models;

namespace ELClass.services
{
    public class AppointmentToLessonWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        public AppointmentToLessonWorker(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

                   
                    var expiredAppointments = await unitOfWork.AppoinmentRepository.GetAsync(
                        a => a.StartDateTime.AddHours(a.DurationInHours) <= DateTime.Now,
                        include: e => e.Include(x => x.StudentAppointments)
                    );

                    foreach (var app in expiredAppointments)
                    {
                        
                        var newLesson = new Lesson
                        {
                            Title = $"Lecture Date: {app.StartDateTime:yyyy-MM-dd}",
                            LectureDate = app.StartDateTime,
                            CourseId = app.CourseId,
                            InstructorId = app.InstructorId ?? "",
                            CreatedAt = DateTime.Now,
                            
                        };

                        await unitOfWork.LessonRepository.CreateAsync(newLesson);

                 
                        if (app.StudentAppointments.Any())
                        {
                            await unitOfWork.StudentAppointmentRepository.DeleteAllAsync(app.StudentAppointments.ToList());
                        }

                
                        await unitOfWork.AppoinmentRepository.DeleteAsync(app);
                    }

                    if (expiredAppointments.Any())
                    {
                        await unitOfWork.CommitAsync();
                    }
                }

                await Task.Delay(TimeSpan.FromMinutes(20), stoppingToken); 
            }
        }
    }
}
