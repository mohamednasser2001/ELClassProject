using System;
using System.Collections.Generic;
using System.Text;

namespace Models.ViewModels
{
    public class StudentChatVM
    {
        public string StudentId { get; set; }
        public string StudentName { get; set; }
        public string LastMessage { get; set; }
        public DateTime LastMessageTime { get; set; }

        // ده العداد اللي هيظهر للمدرس عشان يعرف فيه كام رسالة جديدة من الطالب ده
        public int UnreadCount { get; set; }

        // لو حابب تظهر الحروف الأولى (Initials) زي ما عملنا عند الطالب
        public string Initials => string.IsNullOrWhiteSpace(StudentName) ? "??" :
            string.Concat(StudentName.Split(' ').Select(y => y[0])).ToUpper();
    }
}
