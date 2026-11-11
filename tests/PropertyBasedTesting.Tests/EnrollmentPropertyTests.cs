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

    // ==========================================
    // PBT 1: Inactive Student Property
    // ==========================================
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

    // ==========================================
    // PBT 2: GPA Credit Limit Invariant
    // ==========================================
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

    // ==========================================
    // PBT 7a: Full Course / Capacity Invariant (Task Berli)
    // ==========================================
    [Test]
    public void FullCourse_Rejects_Enrollment()
    {
        var property = Prop.ForAll(
            EnrollmentGenerators.StudentData(),
            EnrollmentGenerators.CourseData(),
            (sData, cData) =>
            {
                var student = new Student("S001", sData.GPA, true, Math.Max(4, sData.MaxAllowedCredits));
                
                // Membuat kelas berkuota 0 (kelas penuh)
                var fullCourse = new Course("C001", "Matkul Test", cData.Credits, 0);

                bool result = _enrollmentService.Enroll(student, fullCourse);

                // Pendaftaran pada kelas berkuota 0 harus selalu ditolak (false)
                return !result;
            });

        Check.QuickThrowOnFailure(property);
    }

    // ==========================================
    // PBT 7b: Duplicate Enrollment / Idempotency (Task Berli)
    // ==========================================
    [Test]
    public void DuplicateEnrollment_IsIdempotent()
    {
        var property = Prop.ForAll(
            EnrollmentGenerators.StudentData(),
            EnrollmentGenerators.CourseData(),
            (sData, cData) =>
            {
                var student = new Student("S001", sData.GPA, true, Math.Max(24, sData.MaxAllowedCredits));
                var course = new Course("C001", "Matkul Test", cData.Credits, Math.Max(1, cData.Capacity));
                var service = new EnrollmentService();

                // Pendaftaran pertama
                bool firstResult = service.Enroll(student, course);
                int creditsAfterFirstEnrollment = student.TotalEnrolledCredits;

                // Pendaftaran kedua dengan objek mata kuliah yang sama persis
                bool secondResult = service.Enroll(student, course);
                int creditsAfterSecondEnrollment = student.TotalEnrolledCredits;

                // Pendaftaran kedua harus selalu ditolak (false) dan total SKS tidak boleh bertambah
                return !secondResult && (creditsAfterFirstEnrollment == creditsAfterSecondEnrollment);
            });

        Check.QuickThrowOnFailure(property);
    }
}