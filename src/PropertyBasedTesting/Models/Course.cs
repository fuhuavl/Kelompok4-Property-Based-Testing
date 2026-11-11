using System;

namespace PropertyBasedTesting.Models;

public class Course
{
    public string CourseId { get; }
    public string Name { get; }
    public int Credits { get; }
    public int Capacity { get; }

    public int EnrolledStudentCount { get; private set; }

    public bool IsFull =>
        EnrolledStudentCount >= Capacity;

    public Course(
        string courseId,
        string name,
        int credits,
        int capacity)
    {
        if (string.IsNullOrWhiteSpace(courseId))
        {
            throw new ArgumentException(
                "Course ID cannot be empty.",
                nameof(courseId));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "Course name cannot be empty.",
                nameof(name));
        }

        if (credits < 1 || credits > 4)
        {
            throw new ArgumentOutOfRangeException(
                nameof(credits),
                "Course credits must be between 1 and 4.");
        }

        if (capacity < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(capacity),
                "Course capacity cannot be negative.");
        }

        CourseId = courseId;
        Name = name;
        Credits = credits;
        Capacity = capacity;

        EnrolledStudentCount = 0;
    }

    public void IncrementEnrollmentCount()
    {
        EnrolledStudentCount++;
    }
}