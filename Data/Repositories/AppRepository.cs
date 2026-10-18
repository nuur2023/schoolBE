using Microsoft.EntityFrameworkCore;
using schoolBE.Data;
using schoolBE.Data.Interfaces;
using schoolBE.Models;
using schoolBE.Models.DTOs;
using schoolBE.ViewModels;
using System.Globalization;

namespace schoolBE.Data.Repositories;

public class AppRepository(SchoolContext context) : IApp
{
    private readonly SchoolContext _context = context;
    private static readonly Random _random = new Random();

    public async Task<StudentEnrollment?> GetStudentByIdAsync(int studentId)
    {
        return await _context.Set<StudentEnrollment>()
            .Include(s => s.School)
            .Include(s => s.Stage)
            .Include(s => s.Level)
            .Include(s => s.Class)
            .FirstOrDefaultAsync(s => s.Id == studentId && s.IsActive);
    }

    public async Task<StudentEnrollment?> GetStudentByGenIdAsync(string genId)
    {
        return await _context.Set<StudentEnrollment>()
            .Include(s => s.School)
            .Include(s => s.Stage)
            .Include(s => s.Level)
            .Include(s => s.Class)
            .FirstOrDefaultAsync(s => s.GenId == genId && s.IsActive);
    }

    public async Task<StudentEnrollment?> GetStudentByGenIdAndSchoolIdAsync(string genId, int schoolId)
    {
        return await _context.Set<StudentEnrollment>()
            .Include(s => s.School)
            .Include(s => s.Stage)
            .Include(s => s.Level)
            .Include(s => s.Class)
            .FirstOrDefaultAsync(s => s.GenId == genId && s.SchoolId == schoolId && s.IsActive);
    }

    public async Task<decimal> GetStudentBalanceAsync(int studentId)
    {
        var student = await _context.Set<StudentEnrollment>()
            .FirstOrDefaultAsync(s => s.Id == studentId);

        if (student == null) return 0;

        var payments = await _context.Set<Payment>()
            .Where(p => p.StudentId == studentId)
            .ToListAsync();

        // Balance = total charged − total paid
        return payments.Sum(p => p.Charged - p.Paid);
    }

    public async Task<decimal> GetStudentBalanceFromJournalEntriesAsync(int studentId)
    {
        // Kept for callers; balances are Payment-based after accounting tables were removed.
        return await GetStudentBalanceAsync(studentId);
    }

    public async Task<int> GetStudentSubjectCountAsync(int studentId)
    {
        var student = await _context.Set<StudentEnrollment>()
            .FirstOrDefaultAsync(s => s.Id == studentId);

        if (student == null) return 0;

        // Get subjects from ClassSubjects where class matches student's class
        var classSubjectsCount = await _context.Set<ClassSubject>()
            .Where(cs => cs.ClassId == student.ClassId)
            .CountAsync();

        // Also get subjects from LevelSubjects where level matches student's level
        var levelSubjectsCount = await _context.Set<LevelSubject>()
            .Where(ls => ls.LevelId == student.LevelId)
            .CountAsync();

        // Return the count (could be union of both, but for now return class subjects)
        return classSubjectsCount > 0 ? classSubjectsCount : levelSubjectsCount;
    }

    public async Task<decimal> GetStudentAttendancePercentageAsync(int studentId, int? month = null)
    {
        var query = _context.Set<Attendance>()
            .Where(a => a.StudentId == studentId);

        if (month.HasValue)
        {
            var year = DateTime.Now.Year;
            query = query.Where(a => a.CreatedAt.Year == year && a.CreatedAt.Month == month.Value);
        }
        else
        {
            // Current month
            var now = DateTime.Now;
            query = query.Where(a => a.CreatedAt.Year == now.Year && a.CreatedAt.Month == now.Month);
        }

        var attendances = await query.ToListAsync();
        
        if (attendances.Count == 0) return 0;

        var presentCount = attendances.Count(a => a.IsPresent);
        var totalCount = attendances.Count;

        if (totalCount == 0) return 0;

        return (decimal)presentCount / totalCount * 100;
    }

    public async Task<(StudentEnrollment? student, List<Contact> contacts)> GetStudentProfileWithContactsAsync(int studentId, int schoolId)
    {
        // Get student with all navigation properties
        var student = await _context.Set<StudentEnrollment>()
            .Include(s => s.School)
            .Include(s => s.Stage)
            .Include(s => s.Level)
            .Include(s => s.Class)
            .FirstOrDefaultAsync(s => s.Id == studentId && s.SchoolId == schoolId && s.IsActive);

        if (student == null)
        {
            return (null, new List<Contact>());
        }

        // Get all contacts for this student
        var contacts = await _context.Set<Contact>()
            .Where(c => c.StudentId == studentId && c.SchoolId == schoolId)
            .ToListAsync();

        return (student, contacts);
    }

    public async Task<TimetableResponse> GetTimetableAsync(int studentId, int schoolId, int levelId, int? classId = null)
    {
        // Get all classes for this level
        var classesQuery = _context.Set<Class>()
            .Where(c => c.LevelId == levelId && c.SchoolId == schoolId && c.IsActive);

        // If classId is provided, filter by it
        if (classId.HasValue)
        {
            classesQuery = classesQuery.Where(c => c.Id == classId.Value);
        }

        var classes = await classesQuery.ToListAsync();
        var classIds = classes.Select(c => c.Id).ToList();

        if (classIds.Count == 0)
        {
            // No classes found for this level/school/class combination
            return new TimetableResponse();
        }

        // Get all class subjects for these classes with navigation properties
        var classSubjects = await _context.Set<ClassSubject>()
            .Include(cs => cs.Subject)
            .Include(cs => cs.Class)
                .ThenInclude(c => c!.Level)
            .Include(cs => cs.Person)
            .Where(cs => classIds.Contains(cs.ClassId) && !string.IsNullOrEmpty(cs.DayOfWeek))
            .OrderBy(cs => cs.DayOfWeek)
            .ThenBy(cs => cs.StartTime)
            .ToListAsync();

        // Group by day of week
        var daysGrouped = classSubjects
            .GroupBy(cs => cs.DayOfWeek)
            .ToList();

        var response = new TimetableResponse();
        var dayOrder = new[] { "Saturday", "Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday" };
        int dayIndex = 1;

        foreach (var dayName in dayOrder)
        {
            var dayGroup = daysGrouped.FirstOrDefault(g => g.Key?.Equals(dayName, StringComparison.OrdinalIgnoreCase) == true);
            if (dayGroup == null) continue;

            var dayDto = new TimetableDayDto
            {
                Id = dayIndex.ToString(),
                Day = dayName,
                Classes = dayGroup.Select((cs, index) => new TimetableClassDto
                {
                    Id = cs.Id.ToString(),
                    Subject = cs.Subject?.Name ?? "Unknown Subject",
                    StartTime = FormatTime(cs.StartTime),
                    EndTime = FormatTime(cs.EndTime),
                    PhotoUrl = string.Empty, // Will be set to default in controller since Person doesn't have Photo
                    Teacher = cs.Person != null ? cs.Person.Name : "No Teacher",
                    Level = cs.Class?.Level?.Name ?? "Unknown Level",
                    Class = cs.Class?.Name ?? "Unknown Class",
                    IsPresent = false, // Default to false, can be calculated based on attendance if needed
                    Color = GenerateRandomColor()
                }).ToList()
            };

            response.Days.Add(dayDto);
            dayIndex++;
        }

        return response;
    }

    private string FormatTime(TimeSpan? time)
    {
        if (!time.HasValue) return string.Empty;

        var dateTime = DateTime.Today.Add(time.Value);
        return dateTime.ToString("hh:mm tt", CultureInfo.InvariantCulture);
    }

    private string GenerateRandomColor()
    {
        // Generate a random color in hex format (0xFFA1B2C3)
        // Flutter accepts colors in format 0xFF followed by 6 hex digits
        var colorValue = _random.Next(0, 0xFFFFFF);
        return $"0xFF{colorValue:X6}";
    }

    public async Task<List<ExamScheduleDto>> GetStudentExamSchedulesAsync(int studentId, int schoolId, int levelId)
    {
        // Get all active and published exam schedules for the given school and level
        // Now ExamSchedule has ExamSubjects, each with LevelSubject
        var examSchedules = await _context.Set<ExamSchedule>()
            .Include(es => es.Exam)
            .Include(es => es.ExamSubjects)
                .ThenInclude(exs => exs.LevelSubject)
                    .ThenInclude(ls => ls!.Subject)
            .Where(es => es.SchoolId == schoolId 
                && es.IsPublished == true
                && es.ExamSubjects.Any(exs => exs.LevelSubject != null && exs.LevelSubject.LevelId == levelId))
            .ToListAsync();

        // Flatten ExamSubjects into individual schedule entries
        var result = new List<ExamScheduleDto>();
        foreach (var schedule in examSchedules)
        {
            var relevantSubjects = schedule.ExamSubjects
                .Where(exs => exs.LevelSubject != null && exs.LevelSubject.LevelId == levelId)
                .OrderBy(exs => exs.ExamDate)
                .ThenBy(exs => exs.StartTime);

            foreach (var examSubject in relevantSubjects)
            {
                result.Add(new ExamScheduleDto
                {
                    Name = schedule.Exam?.Title ?? "Unknown Exam",
                    Subject = examSubject.LevelSubject?.Subject?.Name ?? "Unknown Subject",
                    Date = examSubject.ExamDate.ToString("d-M-yyyy", CultureInfo.InvariantCulture),
                    StartTime = FormatTimeOnly(examSubject.StartTime),
                    EndTime = FormatTimeOnly(examSubject.EndTime),
                    IsActive = true // ExamSubjects are active if the schedule is published
                });
            }
        }

        return result.OrderBy(r => r.Date).ThenBy(r => r.StartTime).ToList();
    }

    private string FormatTimeOnly(TimeSpan timeSpan)
    {
        // Format TimeSpan as "08:15 am" (lowercase am/pm)
        var hours = timeSpan.Hours;
        var minutes = timeSpan.Minutes;
        var amPm = hours >= 12 ? "pm" : "am";
        var displayHours = hours > 12 ? hours - 12 : (hours == 0 ? 12 : hours);
        return $"{displayHours:D2}:{minutes:D2} {amPm}";
    }

    private string FormatTimeOnly(DateTime dateTime)
    {
        // Format as "08:15 am" (lowercase am/pm) - kept for backward compatibility
        return dateTime.ToString("hh:mm tt", CultureInfo.InvariantCulture).ToLower();
    }

    public async Task<List<SubjectWithTeacherDto>> GetSubjectsWithTeachersAsync(int schoolId, int studentId, int levelId, int? classId = null)
    {
        // Get level name
        var level = await _context.Set<Level>()
            .FirstOrDefaultAsync(l => l.Id == levelId && l.SchoolId == schoolId);

        if (level == null)
        {
            return new List<SubjectWithTeacherDto>();
        }

        var levelName = level.Name;

        // Get all LevelSubjects for this level and school
        var levelSubjects = await _context.Set<LevelSubject>()
            .Include(ls => ls.Subject)
            .Where(ls => ls.LevelId == levelId && ls.SchoolId == schoolId && ls.Subject != null && ls.Subject.IsActive)
            .ToListAsync();

        var result = new List<SubjectWithTeacherDto>();

        if (classId.HasValue)
        {
            // Get class name
            var classEntity = await _context.Set<Class>()
                .FirstOrDefaultAsync(c => c.Id == classId.Value && c.LevelId == levelId && c.SchoolId == schoolId);

            var className = classEntity?.Name ?? levelName;

            // Get ClassSubjects for this class with teacher information
            var classSubjects = await _context.Set<ClassSubject>()
                .Include(cs => cs.Subject)
                .Include(cs => cs.Person)
                .Include(cs => cs.Class)
                .Where(cs => cs.ClassId == classId.Value && cs.Subject != null && cs.Subject.IsActive)
                .ToListAsync();

            // Create a dictionary for quick lookup: SubjectId -> ClassSubject (with teacher)
            var classSubjectDict = classSubjects
                .Where(cs => cs.Subject != null)
                .GroupBy(cs => cs.SubjectId)
                .ToDictionary(g => g.Key, g => g.First());

            // Process each LevelSubject
            foreach (var levelSubject in levelSubjects)
            {
                if (levelSubject.Subject == null) continue;

                var subjectName = levelSubject.Subject.Name;
                string? teacherName = null;
                string displayClass = className;

                // Check if there's a ClassSubject with a teacher for this subject
                if (classSubjectDict.TryGetValue(levelSubject.SubjectId, out var classSubject))
                {
                    // Use class name and teacher if available
                    displayClass = classSubject.Class?.Name ?? className;
                    teacherName = classSubject.Person?.Name;
                }
                else
                {
                    // No ClassSubject found, use level name and no teacher
                    displayClass = levelName;
                    teacherName = null;
                }

                result.Add(new SubjectWithTeacherDto
                {
                    Subject = subjectName,
                    Class = displayClass,
                    Teacher = teacherName
                });
            }
        }
        else
        {
            // No classId provided, use level name for all subjects, no teacher info
            foreach (var levelSubject in levelSubjects)
            {
                if (levelSubject.Subject == null) continue;

                result.Add(new SubjectWithTeacherDto
                {
                    Subject = levelSubject.Subject.Name,
                    Class = levelName,
                    Teacher = null
                });
            }
        }

        // Order by subject name
        return result.OrderBy(s => s.Subject).ToList();
    }

    public async Task<List<ExamHistoryDto>> GetExamHistoryAsync(int studentId, int schoolId, int levelId, int? classId = null)
    {
        // Get all exam results with PublicationStatus = "history" for the student
        var examResultsQuery = _context.ExamResults
            .Include(r => r.ExamSubject)
                .ThenInclude(es => es!.LevelSubject)
                    .ThenInclude(ls => ls!.Subject)
            .Include(r => r.ExamSubject)
                .ThenInclude(es => es!.ExamSchedule)
                    .ThenInclude(s => s!.Exam)
            .Include(r => r.StudentEnrollment)
                .ThenInclude(se => se!.Class)
            .Where(r => r.StudentId == studentId &&
                        r.SchoolId == schoolId &&
                        r.ExamSubject != null &&
                        r.ExamSubject.LevelSubject != null &&
                        r.ExamSubject.LevelSubject.LevelId == levelId &&
                        r.PublicationStatus.ToLower() == "history");

        // Filter by classId if provided
        if (classId.HasValue && classId.Value > 0)
        {
            examResultsQuery = examResultsQuery.Where(r => 
                r.StudentEnrollment != null && 
                r.StudentEnrollment.ClassId == classId.Value);
        }

        // Group by AcademicYear, ExamName, Year, and Month
        var groupedResults = await examResultsQuery
            .Where(r => r.ExamSubject!.ExamSchedule!.Exam != null && 
                       r.ExamSubject.LevelSubject!.Subject != null)
            .GroupBy(r => new
            {
                r.AcademicYear,
                ExamName = r.ExamSubject!.ExamSchedule!.Exam!.Title,
                Year = r.ExamSubject.ExamDate.Year,
                Month = r.ExamSubject.ExamDate.Month
            })
            .Select(g => new
            {
                academicYear = g.Key.AcademicYear,
                examName = g.Key.ExamName,
                date = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMMM yyyy", CultureInfo.InvariantCulture),
                fullMarks = g.First().FullMarks,
                average = g.Average(x => x.Percentage),
                results = g.Select(x => new
                {
                    subject = x.ExamSubject!.LevelSubject!.Subject!.Name,
                    marks = x.Marks
                }).ToList()
            })
            .ToListAsync();

        if (groupedResults.Count == 0)
        {
            return new List<ExamHistoryDto>();
        }

        var result = groupedResults
            .Select(g => new ExamHistoryDto
            {
                AcademicYear = g.academicYear,
                ExamName = g.examName,
                Date = g.date,
                FullMarks = g.fullMarks,
                Average = Math.Round(g.average, 2),
                Status = g.average > 49 ? "Passed" : "Failed",
                Results = g.results
                    .Select(r => new ExamHistoryResultDto
                    {
                        Subject = r.subject,
                        Marks = r.marks
                    })
                    .OrderBy(r => r.Subject)
                    .ToList()
            })
            .OrderByDescending(r =>
            {
                // Parse date for sorting (most recent first)
                if (DateTime.TryParseExact(r.Date, "MMMM yyyy", 
                    CultureInfo.InvariantCulture, 
                    System.Globalization.DateTimeStyles.None, out var parsedDate))
                {
                    return parsedDate;
                }
                return DateTime.MinValue;
            })
            .ToList();

        return result;
    }

    public async Task<StudentOfTheYearResponseDto> GetStudentOfTheYearAsync(int schoolId, int levelId, string academicYear)
    {
        // Validate academic year is provided
        if (string.IsNullOrWhiteSpace(academicYear))
        {
            throw new ArgumentException("Academic year is required and cannot be empty", nameof(academicYear));
        }

        var now = DateTime.UtcNow;
        var response = new StudentOfTheYearResponseDto
        {
            SchoolId = schoolId.ToString(),
            LevelId = levelId.ToString(),
            ClassId = null, // No longer using classId
            GeneratedAt = now.ToString("yyyy-MM-dd"),
            Results = new List<StudentOfTheYearResultDto>()
        };

        // Get ALL published exam results for the school and academic year (no level filter yet)
        // AcademicYear format is "2025-2026" (full academic year string like "2025-2026")
        // Filter by "Published" status only to show current year winners in leaderboard
        var allPublishedResultsQuery = _context.ExamResults
            .Include(r => r.ExamSubject)
                .ThenInclude(es => es!.ExamSchedule)
                    .ThenInclude(s => s!.Exam)
            .Include(r => r.ExamSubject)
                .ThenInclude(es => es!.LevelSubject)
                    .ThenInclude(ls => ls!.Level)
            .Include(r => r.StudentEnrollment)
                .ThenInclude(se => se!.Level)
            .Include(r => r.StudentEnrollment)
                .ThenInclude(se => se!.Class)
            .Where(r => r.SchoolId == schoolId &&
                        !string.IsNullOrEmpty(r.AcademicYear) &&
                        r.AcademicYear == academicYear &&
                        r.PublicationStatus == "Published" &&
                        r.StudentEnrollment != null &&
                        r.StudentEnrollment.IsActive &&
                        !r.StudentEnrollment.IsGraduated &&
                        !r.StudentEnrollment.IsLeft &&
                        r.ExamSubject != null &&
                        r.ExamSubject.ExamSchedule != null &&
                        r.ExamSubject.ExamSchedule.Exam != null);

        // Check if any published results exist for the academic year
        var publishedResultsCount = await allPublishedResultsQuery.CountAsync();

        // Remove availability limitation - always show results if they exist
        if (publishedResultsCount == 0)
        {
            // No published results found - return empty but available
            response.IsAvailable = true;
            response.AvailableUntil = now.AddDays(365).ToString("yyyy-MM-dd");
            return response; // Return empty results list
        }

        // Get the most recent publication date for display purposes
        var latestPublicationDate = await allPublishedResultsQuery
            .Where(r => r.UpdatedAt != default || r.Date != default)
            .OrderByDescending(r => r.UpdatedAt != default ? r.UpdatedAt : r.Date)
            .Select(r => r.UpdatedAt != default ? r.UpdatedAt : r.Date)
            .FirstOrDefaultAsync();

        // Set availability - no limitations, always available if results exist
        if (latestPublicationDate != default)
        {
            response.AvailableUntil = latestPublicationDate.AddDays(365).ToString("yyyy-MM-dd");
        }
        else
        {
            response.AvailableUntil = now.AddDays(365).ToString("yyyy-MM-dd");
        }
        
        response.IsAvailable = true; // Always available - no limitations

        // Calculate averages per student for ALL students in the school (no level filter yet)
        // Handle null values in related entities
        var allStudentAverages = await allPublishedResultsQuery
            .GroupBy(r => new
            {
                StudentId = r.StudentId,
                StudentName = r.StudentEnrollment!.Name ?? "Unknown",
                StudentGenId = r.StudentEnrollment.GenId ?? "",
                StudentPhoto = r.StudentEnrollment.Photo,
                LevelId = r.StudentEnrollment.LevelId,
                LevelName = r.StudentEnrollment.Level != null ? r.StudentEnrollment.Level.Name : "Unknown",
                ClassId = (int?)r.StudentEnrollment.ClassId,
                ClassName = r.StudentEnrollment.Class != null ? r.StudentEnrollment.Class.Name : "Unknown"
            })
            .Select(g => new
            {
                g.Key.StudentId,
                g.Key.StudentName,
                g.Key.StudentGenId,
                g.Key.StudentPhoto,
                g.Key.LevelId,
                g.Key.LevelName,
                g.Key.ClassId,
                g.Key.ClassName,
                Average = g.Average(r => (double)r.Percentage)
            })
            .ToListAsync();

        if (!allStudentAverages.Any())
        {
            return response; // No students found
        }

        // ENTIRE SCHOOL: Find max average across all students in the school (calculate first)
        var maxSchoolAverage = allStudentAverages.Max(s => s.Average);
        var schoolWinners = allStudentAverages
            .Where(s => Math.Abs(s.Average - maxSchoolAverage) < 0.01) // Allow for floating point precision
            .ToList();

        // BY LEVEL: Filter students by levelId and find max average within that level
        var levelStudents = allStudentAverages
            .Where(s => s.LevelId == levelId)
            .ToList();

        // Get list of student IDs who won at school level and are in the requested level
        // These students should only appear in SCHOOL scope, not LEVEL scope
        var schoolWinnerIdsInRequestedLevel = schoolWinners
            .Where(sw => sw.LevelId == levelId)
            .Select(sw => sw.StudentId)
            .ToHashSet();

        if (levelStudents.Any())
        {
            var maxLevelAverage = levelStudents.Max(s => s.Average);
            var levelWinners = levelStudents
                .Where(s => Math.Abs(s.Average - maxLevelAverage) < 0.01) // Allow for floating point precision
                .ToList();

            // Filter out students who are also school winners (to avoid duplicates)
            // If a student won both LEVEL and SCHOOL and is in the requested level, show only SCHOOL
            var levelWinnersToShow = levelWinners
                .Where(lw => !schoolWinnerIdsInRequestedLevel.Contains(lw.StudentId))
                .ToList();

            foreach (var winner in levelWinnersToShow)
            {
                response.Results.Add(new StudentOfTheYearResultDto
                {
                    Scope = "LEVEL",
                    ScopeId = levelId.ToString(),
                    Title = $"Student of the Year – {winner.LevelName}",
                    Image = winner.StudentPhoto,
                    FullName = winner.StudentName,
                    MaxAverage = Math.Round(maxLevelAverage, 2),
                    Details = new StudentOfTheYearDetailsDto
                    {
                        StudentId = winner.StudentGenId ?? "",
                        StudentLevelId = winner.LevelName ?? "Unknown",
                        StudentClassId = winner.ClassName ?? "Unknown",
                        Average = Math.Round(maxLevelAverage, 2)
                    }
                });
            }
        }

        // Add school winners (these include students from all levels, including the requested level)
        foreach (var winner in schoolWinners)
        {
            response.Results.Add(new StudentOfTheYearResultDto
            {
                Scope = "SCHOOL",
                ScopeId = schoolId.ToString(),
                Title = "Student of the Year – School",
                Image = winner.StudentPhoto,
                FullName = winner.StudentName,
                MaxAverage = Math.Round(maxSchoolAverage, 2),
                    Details = new StudentOfTheYearDetailsDto
                    {
                        StudentId = winner.StudentGenId ?? "",
                        StudentLevelId = winner.LevelName ?? "Unknown",
                        StudentClassId = winner.ClassName ?? "Unknown",
                        Average = Math.Round(maxSchoolAverage, 2)
                    }
            });
        }

        return response;
    }
}
