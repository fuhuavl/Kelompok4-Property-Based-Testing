using FsCheck;
using FsCheck.Fluent;

namespace PropertyBasedTesting.Tests;

public record StudentData(decimal GPA, bool IsActive, int MaxAllowedCredits);
public record CourseData(int Credits, int Capacity);

public static class EnrollmentGenerators
{
    public static Gen<StudentData> StudentDataGenerator =>
        from gpaValue in Gen.Choose(0, 400)
        from isActive in Gen.Elements(true, false)
        from maxCredits in Gen.Choose(0, 24)
        select new StudentData(gpaValue / 100m, isActive, maxCredits);

    public static Arbitrary<StudentData> StudentData() =>
        Arb.From(StudentDataGenerator, ShrinkStudentData);

    public static Gen<CourseData> CourseDataGenerator =>
        from credits in Gen.Choose(1, 4)
        from capacity in Gen.Choose(0, 100)
        select new CourseData(credits, capacity);

    public static Arbitrary<CourseData> CourseData() =>
        Arb.From(CourseDataGenerator, ShrinkCourseData);


    private static IEnumerable<StudentData> ShrinkStudentData(
        StudentData data)
    {
        if (data.GPA > 0m)
        {
            yield return data with { GPA = 0m };

            yield return data with
            {
                GPA = data.GPA / 2m
            };
        }

        if (data.MaxAllowedCredits > 0)
        {
            yield return data with
            {
                MaxAllowedCredits = 0
            };

            yield return data with
            {
                MaxAllowedCredits = data.MaxAllowedCredits / 2
            };
        }
    }

    private static IEnumerable<CourseData> ShrinkCourseData(
        CourseData data)
    {
        if (data.Credits > 1)
        {
            yield return data with { Credits = 1 };

            yield return data with
            {
                Credits = Math.Max(1, data.Credits / 2)
            };
        }

        if (data.Capacity > 0)
        {
            yield return data with { Capacity = 0 };

            yield return data with
            {
                Capacity = data.Capacity / 2
            };
        }
    }

}