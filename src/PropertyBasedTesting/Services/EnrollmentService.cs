using PropertyBasedTesting.Models;

namespace PropertyBasedTesting.Services;

/// <summary>
/// Handles enrollment rules for students and courses.
/// </summary>
public class EnrollmentService
{
    /// <summary>
    /// Attempts to enroll a student in a course.
    /// Returns false when an enrollment rule prevents registration.
    /// </summary>
    public bool Enroll(Student student, Course course)
    {
        ArgumentNullException.ThrowIfNull(student);
        ArgumentNullException.ThrowIfNull(course);

        if (!student.IsActive)
        {
            return false;
        }

        if (student.IsEnrolledIn(course.CourseId))
        {
            return false;
        }

        if (course.IsFull)
        {
            return false;
        }

        var creditLimit = Math.Min(
            student.MaxAllowedCredits,
            GetCreditLimitFromGpa(student.GPA));

        if (student.TotalEnrolledCredits + course.Credits > creditLimit)
        {
            return false;
        }

        student.AddCourse(course);
        course.IncrementEnrollmentCount();
        return true;
    }

    private static int GetCreditLimitFromGpa(decimal gpa)
    {
        if (gpa >= 3.00m)
        {
            return 24;
        }

        if (gpa >= 2.50m)
        {
            return 21;
        }

        if (gpa >= 2.00m)
        {
            return 18;
        }

        return 12;
    }
}
