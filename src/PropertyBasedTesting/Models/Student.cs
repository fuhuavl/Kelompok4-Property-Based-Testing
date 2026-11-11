using System;
using System.Collections.Generic;
using System.Linq;

namespace PropertyBasedTesting.Models;

public class Student
{
    private readonly List<Course> _enrolledCourses = new();
    private readonly IReadOnlyList<Course> _readOnlyEnrolledCourses;

    public string StudentId { get; }
    public decimal GPA { get; }
    public bool IsActive { get; }
    public int MaxAllowedCredits { get; }

    public IReadOnlyList<Course> EnrolledCourses =>
        _readOnlyEnrolledCourses;

    public int TotalEnrolledCredits =>
        _enrolledCourses.Sum(course => course.Credits);

    public Student(
        string studentId,
        decimal gpa,
        bool isActive,
        int maxAllowedCredits)
    {
        if (string.IsNullOrWhiteSpace(studentId))
        {
            throw new ArgumentException(
                "Student ID cannot be empty.",
                nameof(studentId));
        }

        if (gpa < 0.00m || gpa > 4.00m)
        {
            throw new ArgumentOutOfRangeException(
                nameof(gpa),
                "GPA must be between 0.00 and 4.00.");
        }

        if (maxAllowedCredits < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxAllowedCredits),
                "Maximum credits cannot be negative.");
        }

        StudentId = studentId;
        GPA = gpa;
        IsActive = isActive;
        MaxAllowedCredits = maxAllowedCredits;

        _readOnlyEnrolledCourses =
            _enrolledCourses.AsReadOnly();
    }

    public bool IsEnrolledIn(string courseId)
    {
        return _enrolledCourses.Any(
            course => course.CourseId == courseId);
    }

    internal void AddCourse(Course course)
    {
        ArgumentNullException.ThrowIfNull(course);
        _enrolledCourses.Add(course);
    }
}
