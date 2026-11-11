using System;
using FsCheck;
using FsCheck.Fluent;
using NUnit.Framework;
using PropertyBasedTesting.Models;
using PropertyBasedTesting.Services;

namespace PropertyBasedTesting.Tests;

[TestFixture]
public class EnrollmentPropertyTests
{
    private EnrollmentService _enrollmentService;

    [SetUp]
    public void Setup()
    {
        _enrollmentService = new EnrollmentService();
    }

    [Test]
    public void InactiveStudent_CanNeverEnroll()
    {
        var property = Prop.ForAll(
            EnrollmentGenerators.StudentData(),
            EnrollmentGenerators.CourseData(),
            (sData, cData) =>
            {
                var student = new Student("S001", sData.GPA, false, Math.Max(4, sData.MaxAllowedCredits));
                var course = new Course("C001", "Matkul Test", cData.Credits, Math.Max(1, cData.Capacity));

                bool result = _enrollmentService.Enroll(student, course);

                return !result;
            });

        Check.QuickThrowOnFailure(property);
    }

    [Test]
    public void EnrolledCredits_NeverExceeds_GPA_Limit()
    {
        var property = Prop.ForAll(
            EnrollmentGenerators.StudentData(),
            EnrollmentGenerators.CourseData(),
            (sData, cData) =>
            {
                var student = new Student("S001", sData.GPA, true, sData.MaxAllowedCredits);
                var service = new EnrollmentService();

                int creditLimitFromGpa = sData.GPA switch
                {
                    >= 3.00m => 24,
                    >= 2.50m => 21,
                    >= 2.00m => 18,
                    _ => 12
                };

                int effectiveLimit = Math.Min(
                    sData.MaxAllowedCredits,
                    creditLimitFromGpa
                );

                for (int i = 0; i < 30; i++)
                {
                    var course = new Course(
                        $"C{i + 1}",
                        $"Matkul Test {i + 1}",
                        cData.Credits,
                        1
                    );

                    bool expectedResult =
                        student.TotalEnrolledCredits + course.Credits
                        <= effectiveLimit;

                    bool actualResult = service.Enroll(student, course);

                    if (actualResult != expectedResult)
                    {
                        return false;
                    }

                    if (student.TotalEnrolledCredits > effectiveLimit)
                    {
                        return false;
                    }
                }

                return true;
            });

        Check.QuickThrowOnFailure(property);
    }
}