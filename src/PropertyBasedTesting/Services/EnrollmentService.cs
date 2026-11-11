using System;
using PropertyBasedTesting.Models;

namespace PropertyBasedTesting.Services;

public class EnrollmentService
{
    public bool Enroll(Student student, Course course)
    {
        ArgumentNullException.ThrowIfNull(student);
        ArgumentNullException.ThrowIfNull(course);

        if (!student.IsActive)
        {
            return false;
        }

        if (course.Capacity <= 0 || course.IsFull)
        {
            return false;
        }

        if (student.IsEnrolledIn(course.CourseId))
        {
            return false;
        }

        int creditLimitFromGpa = student.GPA switch
        {
            >= 3.00m => 24,
            >= 2.50m => 21,
            >= 2.00m => 18,
            _ => 12
        };

        int effectiveLimit = Math.Min(student.MaxAllowedCredits, creditLimitFromGpa);

        if (student.TotalEnrolledCredits + course.Credits > effectiveLimit)
        {
            return false;
        }

        student.AddCourse(course);
        course.IncrementEnrollmentCount();

        return true;
    }
}