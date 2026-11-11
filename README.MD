
![.NET](https://img.shields.io/badge/.NET-10.0-blue)
![C%23](https://img.shields.io/badge/C%23-14-purple)
![NUnit](https://img.shields.io/badge/NUnit-4.x-green)
![FsCheck](https://img.shields.io/badge/FsCheck-3.0.0-orange)

# Property-Based Testing dengan FsCheck dan NUnit

Tugas ini mengeksplorasi metode pengujian **Property-Based Testing (PBT)** menggunakan **C#**, **.NET 10**, **FsCheck**, dan **NUnit**. Studi kasus yang digunakan adalah **Sistem Pengambilan Mata Kuliah (Course Enrollment System / KRS)**. Berbeda dengan *Example-Based Testing* yang menentukan input-output secara spesifik/manual, PBT memverifikasi **invariants** (kebenaran universal) terhadap berbagai variasi data acak yang dibangkitkan secara otomatis.

# Kelompok 4

- Alhaura Rahmatunisa Harsanto 5053241046
- Annisa Zahra Fitria 5053241016
- Berlian Yafi 5053241046 
- Fathiya Haya Shafa Kamila Setiadi 5053241047

## Lingkungan Pengembangan

- **IDE:** Visual Studio 2026 / Visual Studio Code (WSL)
- **SDK:** .NET 10.0
- **Bahasa:** C# 14
- **Test Framework:** NUnit 4.x
- **Property-Based Testing Tool:** FsCheck (FsCheck.NUnit & FsCheck.Fluent)
- **Test Runner:** Microsoft.NET.Test.Sdk dan NUnit3TestAdapter

Perintah utama dari direktori utama repository:

```bash
dotnet restore
dotnet build
dotnet test

```

Struktur Solution:

```text
Kelompok4-Property-Based-Testing/
├── PropertyBasedTesting.slnx
├── README.md
├── src/
│   └── PropertyBasedTesting/
│       ├── Models/
│       │   ├── Student.cs
│       │   └── Course.cs
│       ├── Services/
│       │   └── EnrollmentService.cs
│       ├── Program.cs
│       └── PropertyBasedTesting.csproj
└── tests/
    └── PropertyBasedTesting.Tests/
        ├── EnrollmentGenerators.cs
        ├── EnrollmentPropertyTests.cs
        ├── UnitTest1.cs
        └── PropertyBasedTesting.Tests.csproj

```

Ketentuan umum:

* Folder `src/` berisi kode domain produksi (Mahasiswa, Mata Kuliah, dan Layanan Pendaftaran).
* Folder `tests/` digunakan untuk menulis *data generators* dan *property tests* menggunakan FsCheck.
* Seluruh pengujian *property* dieksekusi secara otomatis melalui perintah `dotnet test`.

---

## Alur Pengerjaan & Pengujian Properti 

### 1. Setup Environment & Solution

Langkah awal pengerjaan dimulai dengan menyiapkan struktur proyek .NET 10, menghubungkan project referensi, dan memasang library NuGet yang diperlukan:

```bash
# Menambahkan NuGet Package pada test project
dotnet add tests/PropertyBasedTesting.Tests package FsCheck
dotnet add tests/PropertyBasedTesting.Tests package FsCheck.NUnit
dotnet add tests/PropertyBasedTesting.Tests package NUnit
dotnet add tests/PropertyBasedTesting.Tests package NUnit3TestAdapter

# Verifikasi build awal
dotnet restore
dotnet build
dotnet test

```

---

### 2. Implementation: Core Domain Models

Domain model menyimpan data dasar mahasiswa dan mata kuliah beserta enkapsulasi aturan pembacaan state internal.

`src/PropertyBasedTesting/Models/Course.cs`

```csharp
using System;

namespace PropertyBasedTesting.Models;

public class Course
{
    public string CourseId { get; }
    public string Name { get; }
    public int Credits { get; }
    public int Capacity { get; }

    public int EnrolledStudentCount { get; private set; }

    public bool IsFull => EnrolledStudentCount >= Capacity;

    public Course(string courseId, string name, int credits, int capacity)
    {
        if (string.IsNullOrWhiteSpace(courseId))
            throw new ArgumentException("Course ID cannot be empty.", nameof(courseId));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Course name cannot be empty.", nameof(name));
        if (credits < 1 || credits > 4)
            throw new ArgumentOutOfRangeException(nameof(credits), "Course credits must be between 1 and 4.");
        if (capacity < 0)
            throw new ArgumentOutOfRangeException(nameof(capacity), "Course capacity cannot be negative.");

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

```

`src/PropertyBasedTesting/Models/Student.cs`

```csharp
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

    public IReadOnlyList<Course> EnrolledCourses => _readOnlyEnrolledCourses;
    public int TotalEnrolledCredits => _enrolledCourses.Sum(course => course.Credits);

    public Student(string studentId, decimal gpa, bool isActive, int maxAllowedCredits)
    {
        if (string.IsNullOrWhiteSpace(studentId))
            throw new ArgumentException("Student ID cannot be empty.", nameof(studentId));
        if (gpa < 0.00m || gpa > 4.00m)
            throw new ArgumentOutOfRangeException(nameof(gpa), "GPA must be between 0.00 and 4.00.");
        if (maxAllowedCredits < 0)
            throw new ArgumentOutOfRangeException(nameof(maxAllowedCredits), "Maximum credits cannot be negative.");

        StudentId = studentId;
        GPA = gpa;
        IsActive = isActive;
        MaxAllowedCredits = maxAllowedCredits;
        _readOnlyEnrolledCourses = _enrolledCourses.AsReadOnly();
    }

    public bool IsEnrolledIn(string courseId)
    {
        return _enrolledCourses.Any(course => course.CourseId == courseId);
    }

    public void AddCourse(Course course)
    {
        ArgumentNullException.ThrowIfNull(course);
        _enrolledCourses.Add(course);
    }
}

```

---

### 3. Implementation: EnrollmentService

`EnrollmentService` mengelola logika pendaftaran KRS dengan menerapkan aturan validasi keaktifan, ketersediaan kuota, pendaftaran ganda, serta batas SKS berbasis IPK.

`src/PropertyBasedTesting/Services/EnrollmentService.cs`

```csharp
using System;
using PropertyBasedTesting.Models;

namespace PropertyBasedTesting.Services;

public class EnrollmentService
{
    public bool Enroll(Student student, Course course)
    {
        ArgumentNullException.ThrowIfNull(student);
        ArgumentNullException.ThrowIfNull(course);

        // 1. Validasi Status Keaktifan Mahasiswa (PBT 1)
        if (!student.IsActive) return false;

        // 2. Validasi Kuota Kelas / Class Capacity Invariant (PBT 7a)
        if (course.Capacity <= 0 || course.IsFull) return false;

        // 3. Validasi Duplicate Course / Idempotency Property (PBT 7b)
        if (student.IsEnrolledIn(course.CourseId)) return false;

        // 4. Hitung Batas Maksimal SKS Berdasarkan IPK
        int creditLimitFromGpa = student.GPA switch
        {
            >= 3.00m => 24,
            >= 2.50m => 21,
            >= 2.00m => 18,
            _ => 12
        };

        int effectiveLimit = Math.Min(student.MaxAllowedCredits, creditLimitFromGpa);

        // 5. Validasi Batas Maksimal SKS (PBT 2)
        if (student.TotalEnrolledCredits + course.Credits > effectiveLimit) return false;

        // 6. Eksekusi Pendaftaran
        student.AddCourse(course);
        course.IncrementEnrollmentCount();

        return true;
    }
}

```

---

### 4. Implementation: Custom Data Generators

`EnrollmentGenerators` bertugas membangkitkan data acak mahasiswa (`StudentData`) dan mata kuliah (`CourseData`) secara rasional agar FsCheck dapat melakukan pengujian properti secara komprehensif.

`tests/PropertyBasedTesting.Tests/EnrollmentGenerators.cs`

```csharp
using FsCheck;
using PropertyBasedTesting.Models;

namespace PropertyBasedTesting.Tests;

public record StudentData(decimal GPA, int MaxAllowedCredits);
public record CourseData(int Credits, int Capacity);

public static class EnrollmentGenerators
{
    public static Arbitrary<StudentData> StudentData()
    {
        var gen = from gpaDouble in Gen.Choose(0, 400)
                  let gpa = (decimal)gpaDouble / 100m
                  from maxCredits in Gen.Choose(12, 24)
                  select new StudentData(gpa, maxCredits);

        return gen.ToArbitrary();
    }

    public static Arbitrary<CourseData> CourseData()
    {
        var gen = from credits in Gen.Choose(1, 4)
                  from capacity in Gen.Choose(0, 50)
                  select new CourseData(credits, capacity);

        return gen.ToArbitrary();
    }
}

```

---

### 5. Property 1: Inactive Student Property

**Spesifikasi:** Mahasiswa dengan status non-aktif (`IsActive == false`) tidak boleh berhasil mengambil mata kuliah apa pun, berapa pun nilai IPK dan kuota kelasnya.

File: `tests/PropertyBasedTesting.Tests/EnrollmentPropertyTests.cs`

```csharp
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

```

---

### 6. Property 2: GPA Credit Limit Invariant

**Spesifikasi:** Total SKS yang diambil mahasiswa tidak boleh melebihi batas SKS yang diizinkan berdasarkan rentang IPK-nya ($\text{IPK} \ge 3.00 \implies 24$, $\ge 2.50 \implies 21$, $\ge 2.00 \implies 18$, $< 2.00 \implies 12$).

File: `tests/PropertyBasedTesting.Tests/EnrollmentPropertyTests.cs`

```csharp
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

            int effectiveLimit = Math.Min(sData.MaxAllowedCredits, creditLimitFromGpa);

            for (int i = 0; i < 30; i++)
            {
                var course = new Course($"C{i + 1}", $"Matkul Test {i + 1}", cData.Credits, 1);

                bool expectedResult = student.TotalEnrolledCredits + course.Credits <= effectiveLimit;
                bool actualResult = service.Enroll(student, course);

                if (actualResult != expectedResult || student.TotalEnrolledCredits > effectiveLimit)
                {
                    return false;
                }
            }

            return true;
        });

    Check.QuickThrowOnFailure(property);
}

```

---

### 7. Property 3: Capacity & Idempotency Properties

**Spesifikasi:**

* **`FullCourse_Rejects_Enrollment`:** Pendaftaran pada kelas yang sudah penuh (`Capacity == 0` atau `IsFull`) pasti selalu ditolak.
* **`DuplicateEnrollment_IsIdempotent`:** Mendaftarkan mata kuliah yang sama dua kali tidak akan menambah total SKS atau mengubah status pada upaya kedua.

File: `tests/PropertyBasedTesting.Tests/EnrollmentPropertyTests.cs`

```csharp
[Test]
public void FullCourse_Rejects_Enrollment()
{
    var property = Prop.ForAll(
        EnrollmentGenerators.StudentData(),
        EnrollmentGenerators.CourseData(),
        (sData, cData) =>
        {
            var student = new Student("S001", sData.GPA, true, Math.Max(4, sData.MaxAllowedCredits));
            var fullCourse = new Course("C001", "Matkul Test", cData.Credits, 0);

            bool result = _enrollmentService.Enroll(student, fullCourse);

            return !result;
        });

    Check.QuickThrowOnFailure(property);
}

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

            bool firstResult = service.Enroll(student, course);
            int creditsAfterFirst = student.TotalEnrolledCredits;

            bool secondResult = service.Enroll(student, course);
            int creditsAfterSecond = student.TotalEnrolledCredits;

            return !secondResult && (creditsAfterFirst == creditsAfterSecond);
        });

    Check.QuickThrowOnFailure(property);
}

```

---

### 8. Executing Property Tests

Seluruh pengujian dapat dieksekusi dengan perintah `dotnet test`. FsCheck secara *default* membangkitkan **100 permutasi data uji acak** untuk setiap method properti.

```bash
dotnet test

```

Output eksekusi ketika seluruh pengujian berhasil:

```text
Passed!  - Failed:     0, Passed:     4, Skipped:     0, Total:     4, Duration: 1.2 s

```

---

### 9. Intentional Bug Simulation

Untuk mendemonstrasikan keandalan FsCheck dalam menangkap pelanggaran invariant, kami mensimulasikan *bug* pada `Services/EnrollmentService.cs` dengan menonaktifkan pengecekan kuota kelas:

```csharp
// INTENTIONAL BUG: Pengecekan kuota kelas dinonaktifkan
// if (course.Capacity <= 0 || course.IsFull) return false;

```

---

### 10. Testing & Failure Analysis (Shrinking & Replay Seed)

Saat pengujian dijalankan pada kode yang memiliki *bug*, FsCheck secara otomatis mengisolasi kegagalan dan melakukan **Feature Shrinking** untuk mencari *minimal counterexample*.

Jalankan perintah berikut:

```bash
dotnet test --filter "FullyQualifiedName~FullCourse_Rejects_Enrollment"

```

Output analisis kegagalan yang dihasilkan oleh FsCheck:

```text
Failed FullCourse_Rejects_Enrollment [18 ms]
  Error Message:
    System.Exception : Falsifiable, after 1 test (0 shrinks) (StdGen (14285731, 296188489)):
    Original Input:
    StudentData { GPA = 3.50, MaxAllowedCredits = 24 }
    CourseData { Credits = 3, Capacity = 0 }

```

**Analisis Output Kegagalan:**

1. **Falsifiable:** FsCheck membuktikan bahwa invariant properti dilanggar karena operasi `Enroll` mengembalikan nilai `true` untuk kelas berkuota 0.
2. **Feature Shrinking:** FsCheck menyederhanakan kombinasi input awal ke kasus uji paling sederhana yang tetap menyebabkan sistem mengalami kegagalan.
3. **Replayability:** Nilai `StdGen (14285731, 296188489)` dapat dipasang sebagai seed acak deterministik untuk mereproduksi kegagalan secara konsisten saat perbaikan kode dilakukan.

---

### 11. Bug Fixing & Full Test Verification

Setelah *bug* pada `EnrollmentService.cs` diperbaiki (dengan mengaktifkan kembali validasi kuota `course.Capacity <= 0 || course.IsFull`), jalankan pembersihan dan pengujian ulang:

```bash
dotnet clean
dotnet build
dotnet test

```

Seluruh pengujian dipastikan berstatus **Passed 100%**.
