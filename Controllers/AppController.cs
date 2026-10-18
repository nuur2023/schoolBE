using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using schoolBE.Constants;
using schoolBE.Data.Interfaces;
using schoolBE.Models;
using schoolBE.Models.DTOs;
using schoolBE.Utils;
using schoolBE.ViewModels;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace schoolBE.Controllers;

[ApiController]
[Route("api/App")]
[EnableCors(policyName: "CorePolicy")]
public class AppController : ControllerBase
{
    private readonly IApp _appRepository;
    private readonly IStudentEnrollments _studentEnrollmentsRepository;
    private readonly IExamResults _examResultsRepository;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AppController> _logger;

    public AppController(
        IApp appRepository, 
        IStudentEnrollments studentEnrollmentsRepository,
        IExamResults examResultsRepository,
        IConfiguration configuration,
        ILogger<AppController> logger)
    {
        _appRepository = appRepository;
        _studentEnrollmentsRepository = studentEnrollmentsRepository;
        _examResultsRepository = examResultsRepository;
        _configuration = configuration;
        _logger = logger;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult> StudentLogin([FromBody] StudentLoginRequest? request)
    {
        try
        {
            if (request == null)
            {
                _logger.LogWarning("Login attempt with null request body");
                return BadRequest(new { message = "Request body is required" });
            }

            if (string.IsNullOrEmpty(request.GenId) || string.IsNullOrEmpty(request.Password))
            {
                _logger.LogWarning("Login attempt with missing GenId or Password");
                return BadRequest(new { message = "GenId and password are required" });
            }

            // Get student by GenId directly from database
            var student = await _appRepository.GetStudentByGenIdAsync(request.GenId);

            if (student == null)
            {
                _logger.LogWarning("Login attempt with invalid GenId: {GenId}", request.GenId);
                return Unauthorized(new { message = "Invalid Student ID or password" });
            }

            // Check if student is active
            if (!student.IsActive)
            {
                _logger.LogWarning("Login attempt for inactive student: {GenId}", request.GenId);
                return Unauthorized(new { message = "Student account is inactive" });
            }

            // Verify password - check if it's hashed or plain text
            bool passwordValid = false;
            if (!string.IsNullOrEmpty(student.Password))
            {
                // Try to verify as hashed password first
                passwordValid = Helper.VerifyPassword(request.Password, student.Password);
                
                // If hashed verification fails, try plain text comparison (for backward compatibility)
                if (!passwordValid)
                {
                    passwordValid = student.Password == request.Password;
                }
            }

            if (!passwordValid)
            {
                _logger.LogWarning("Login attempt with invalid password for GenId: {GenId}", request.GenId);
                return Unauthorized(new { message = "Invalid Student ID or password" });
            }

            // Generate JWT token for student
            var token = GenerateStudentJwtToken(student);

            // Set secure cookie with token
            var cookieOptions = new Microsoft.AspNetCore.Http.CookieOptions
            {
                HttpOnly = true,
                Secure = false, // false for HTTP in development
                SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax,
                Expires = DateTime.UtcNow.AddMinutes(120),
                Path = "/"
            };

            Response.Cookies.Append("authToken", token, cookieOptions);

            // Return student data (without password)
            var response = new
            {
                success = true,
                token = token,
                studentId = student.Id,
                genId = student.GenId,
                name = student.Name,
                schoolId = student.SchoolId,
                schoolName = student.School?.Name ?? string.Empty, // Add schoolName for mobile app
                stageId = student.StageId,
                levelId = student.LevelId,
                classId = student.ClassId,
                photo = student.Photo,
                currentAcademicYear = student.CurrentAcademicYear,
                dob = student.DOB,
                gender = student.Gender,
                school = student.School != null ? new
                {
                    id = student.School.Id,
                    name = student.School.Name
                } : null,
                stage = student.Stage != null ? new
                {
                    id = student.Stage.Id,
                    name = student.Stage.Name
                } : null,
                level = student.Level != null ? new
                {
                    id = student.Level.Id,
                    name = student.Level.Name
                } : null,
                @class = student.Class != null ? new
                {
                    id = student.Class.Id,
                    name = student.Class.Name
                } : null
            };

            _logger.LogInformation("Student login successful for GenId: {GenId}, StudentId: {StudentId}", request.GenId, student.Id);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during student login for GenId: {GenId}. Error: {ErrorMessage}", request?.GenId ?? "unknown", ex.Message);
            return StatusCode(500, new { message = "An error occurred during login", error = ex.Message });
        }
    }

    [HttpGet("dashboard")]
    [Authorize]
    public async Task<ActionResult<StudentDashboardResponse>> GetDashboard([FromQuery] int studentId, [FromQuery] int schoolId)
    {
        try
        {
            // Validate token and ensure the student ID in token matches the requested student ID
            var tokenStudentId = GetCurrentStudentId();
            if (tokenStudentId != studentId)
            {
                _logger.LogWarning("Token student ID ({TokenStudentId}) does not match requested student ID ({StudentId})", tokenStudentId, studentId);
                return Unauthorized(new { message = "You can only access your own dashboard" });
            }

            // Validate parameters
            if (studentId <= 0 || schoolId <= 0)
            {
                return BadRequest(new { message = "Student ID and School ID are required and must be greater than 0" });
            }

            var student = await _appRepository.GetStudentByIdAsync(studentId);

            if (student == null)
            {
                return NotFound(new { message = "Student not found" });
            }

            // Verify the student belongs to the requested school
            if (student.SchoolId != schoolId)
            {
                _logger.LogWarning("Student {StudentId} does not belong to school {SchoolId}", studentId, schoolId);
                return BadRequest(new { message = "Student does not belong to the specified school" });
            }

            // Get dashboard data sequentially (DbContext doesn't support concurrent operations)
            var attendancePercentage = await _appRepository.GetStudentAttendancePercentageAsync(studentId);
            var subjectCount = await _appRepository.GetStudentSubjectCountAsync(studentId);
            var balance = await _appRepository.GetStudentBalanceAsync(studentId);

            // Format photo URL - Photo field now only contains filename
            var photoUrl = string.Empty;
            if (!string.IsNullOrEmpty(student.Photo))
            {
                // Sanitize filename to prevent path traversal
                var sanitizedFileName = System.IO.Path.GetFileName(student.Photo);
                photoUrl = $"{Request.Scheme}://{Request.Host}{Request.PathBase}/Images/{sanitizedFileName}";
            }
            else
            {
                photoUrl = $"{Request.Scheme}://{Request.Host}{Request.PathBase}/Images/default.png";
            }

            // Build response as a single object
            var response = new StudentDashboardResponse
            {
                StudentInfo = new StudentInfoDto
                {
                    Name = student.Name,
                    GenId = student.GenId,
                    Level = student.Level?.Name ?? string.Empty,
                    Grade = student.Class?.Name ?? string.Empty, // TODO: Calculate grade from exam results if needed
                    PhotoUrl = photoUrl,
                    IsActiveInApp = student.IsActiveInApp
                },
                Attendance = new AttendanceDto
                {
                    Name = "Monthly",
                    SubName = "Attendance",
                    AttendancePercentage = attendancePercentage
                },
                Subjects = new SubjectsDto
                {
                    Name = "Class",
                    SubName = "subjects",
                    Number = subjectCount
                },
                Balance = new BalanceDto
                {
                    Name = "Student",
                    SubName = "Balance",
                    Amount = balance
                }
            };

            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("Unauthorized dashboard access attempt: {Message}", ex.Message);
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting dashboard data for student");
            return StatusCode(500, new { message = "An error occurred while fetching dashboard data", error = ex.Message });
        }
    }

    [HttpGet("profile")]
    [Authorize]
    public async Task<ActionResult<StudentProfileResponse>> GetStudentProfile([FromQuery] int studentId, [FromQuery] int schoolId)
    {
        try
        {
            // Validate token and ensure the student ID in token matches the requested student ID
            var tokenStudentId = GetCurrentStudentId();
            if (tokenStudentId != studentId)
            {
                _logger.LogWarning("Token student ID ({TokenStudentId}) does not match requested student ID ({StudentId})", tokenStudentId, studentId);
                return Unauthorized(new { message = "You can only access your own profile" });
            }

            // Validate parameters
            if (studentId <= 0 || schoolId <= 0)
            {
                return BadRequest(new { message = "Student ID and School ID are required and must be greater than 0" });
            }

            // Get student profile with contacts
            var (student, contacts) = await _appRepository.GetStudentProfileWithContactsAsync(studentId, schoolId);

            if (student == null)
            {
                return NotFound(new { message = "Student not found" });
            }

            // Format photo URL - Photo field now only contains filename
            var photoUrl = string.Empty;
            if (!string.IsNullOrEmpty(student.Photo))
            {
                // Sanitize filename to prevent path traversal
                var sanitizedFileName = System.IO.Path.GetFileName(student.Photo);
                photoUrl = $"{Request.Scheme}://{Request.Host}{Request.PathBase}/Images/{sanitizedFileName}";
            }
            else
            {
                photoUrl = $"{Request.Scheme}://{Request.Host}{Request.PathBase}/Images/default.png";
            }

            // Build response
            var response = new StudentProfileResponse
            {
                Student = new StudentProfileDto
                {
                    Id = student.Id,
                    Name = student.Name,
                    GenId = student.GenId,
                    DOB = student.DOB,
                    POB = student.POB,
                    Gender = student.Gender,
                    Address = student.Address,
                    Mother = student.Mother,
                    PhotoUrl = photoUrl,
                    CurrentAcademicYear = student.CurrentAcademicYear,
                    IsActive = student.IsActive,
                    IsActiveInApp = student.IsActiveInApp,
                    School = student.School?.Name,
                    Stage = student.Stage?.Name,
                    Level = student.Level?.Name,
                    Class = student.Class?.Name
                },
                Contacts = contacts.Select(c => new ContactDto
                {
                    Id = c.Id,
                    Phone = c.Phone,
                    SchoolId = c.SchoolId
                }).ToList()
            };

            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("Unauthorized profile access attempt: {Message}", ex.Message);
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting student profile for student {StudentId}", studentId);
            return StatusCode(500, new { message = "An error occurred while fetching student profile", error = ex.Message });
        }
    }

    [HttpGet("timetable")]
    [Authorize]
    public async Task<ActionResult<List<TimetableDayDto>>> GetTimetable([FromQuery] int studentId, [FromQuery] int schoolId, [FromQuery] int levelId, [FromQuery] int? classId = null)
    {
        try
        {
            // Validate token and ensure the student ID in token matches the requested student ID
            var tokenStudentId = GetCurrentStudentId();
            if (tokenStudentId != studentId)
            {
                _logger.LogWarning("Token student ID ({TokenStudentId}) does not match requested student ID ({StudentId})", tokenStudentId, studentId);
                return Unauthorized(new { message = "You can only access your own timetable" });
            }

            // Validate parameters
            if (studentId <= 0 || schoolId <= 0 || levelId <= 0)
            {
                return BadRequest(new { message = "Student ID, School ID, and Level ID are required and must be greater than 0" });
            }

            // Get timetable data
            var timetable = await _appRepository.GetTimetableAsync(studentId, schoolId, levelId, classId);

            if (timetable == null)
            {
                _logger.LogWarning("Timetable is null for student {StudentId}, school {SchoolId}, level {LevelId}, class {ClassId}", 
                    studentId, schoolId, levelId, classId);
                return Ok(new List<TimetableDayDto>());
            }

            // Build photo URLs for teachers (using default since Person doesn't have Photo property)
            var baseUrl = $"{Request.Scheme}://{Request.Host}{Request.PathBase}";
            if (timetable.Days != null)
            {
                foreach (var day in timetable.Days)
                {
                    if (day?.Classes != null)
                    {
                        foreach (var classItem in day.Classes)
                        {
                            // Set default photo URL since Person model doesn't have Photo property
                            classItem.PhotoUrl = $"{baseUrl}/Images/default.png";
                        }
                    }
                }
            }

            // Return just the days array as requested
            return Ok(timetable.Days ?? new List<TimetableDayDto>());
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("Unauthorized timetable access attempt: {Message}", ex.Message);
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting timetable for student {StudentId}", studentId);
            return StatusCode(500, new { message = "An error occurred while fetching timetable", error = ex.Message });
        }
    }

    [HttpGet("exam-schedules")]
    [Authorize]
    public async Task<ActionResult<List<ExamScheduleDto>>> GetExamSchedules(
        [FromQuery] int studentId,
        [FromQuery] int schoolId,
        [FromQuery] int levelId)
    {
        try
        {
            // Validate token and ensure the student ID in token matches the requested student ID
            var tokenStudentId = GetCurrentStudentId();
            if (tokenStudentId != studentId)
            {
                _logger.LogWarning("Token student ID ({TokenStudentId}) does not match requested student ID ({StudentId})", tokenStudentId, studentId);
                return Unauthorized(new { message = "You can only access your own exam schedules" });
            }

            // Validate parameters
            if (studentId <= 0 || schoolId <= 0 || levelId <= 0)
            {
                return BadRequest(new { message = "Student ID, School ID, and Level ID are required and must be greater than 0" });
            }

            // Get exam schedules
            var schedules = await _appRepository.GetStudentExamSchedulesAsync(studentId, schoolId, levelId);

            return Ok(schedules);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("Unauthorized exam schedules access attempt: {Message}", ex.Message);
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting exam schedules for student {StudentId}", studentId);
            return StatusCode(500, new { message = "An error occurred while fetching exam schedules", error = ex.Message });
        }
    }

    [HttpGet("exam-results")]
    [Authorize]
    public async Task<ActionResult<List<StudentExamResultsResponse>>> GetExamResults(
        [FromQuery] int studentId,
        [FromQuery] int schoolId)
    {
        try
        {
            // Validate token and ensure the student ID in token matches the requested student ID
            var tokenStudentId = GetCurrentStudentId();
            if (tokenStudentId != studentId)
            {
                _logger.LogWarning("Token student ID ({TokenStudentId}) does not match requested student ID ({StudentId})", tokenStudentId, studentId);
                return Unauthorized(new { message = "You can only access your own exam results" });
            }

            // Validate parameters
            if (studentId <= 0 || schoolId <= 0)
            {
                return BadRequest(new { message = "Student ID and School ID are required and must be greater than 0" });
            }

            // Get exam results
            var result = await _examResultsRepository.GetStudentExamResultsAsync(studentId, schoolId);
            
            if (result == null)
            {
                return NotFound(new { message = $"Student with ID {studentId} not found in school {schoolId}" });
            }

            // Format photo URL if present
            if (!string.IsNullOrEmpty(result.Image))
            {
                var sanitizedFileName = System.IO.Path.GetFileName(result.Image);
                result.Image = $"{Request.Scheme}://{Request.Host}{Request.PathBase}/Images/{sanitizedFileName}";
            }
            else
            {
                result.Image = $"{Request.Scheme}://{Request.Host}{Request.PathBase}/Images/default.png";
            }

            // Return as array to match mobile app structure
            return Ok(new List<StudentExamResultsResponse> { result });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("Unauthorized exam results access attempt: {Message}", ex.Message);
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting exam results for student {StudentId}", studentId);
            return StatusCode(500, new { message = "An error occurred while fetching exam results", error = ex.Message });
        }
    }

    [HttpGet("exam-history")]
    [Authorize]
    public async Task<ActionResult<List<ExamHistoryDto>>> GetExamHistory(
        [FromQuery] int studentId,
        [FromQuery] int schoolId,
        [FromQuery] int levelId,
        [FromQuery] int? classId = null)
    {
        try
        {
            // Validate token and ensure the student ID in token matches the requested student ID
            var tokenStudentId = GetCurrentStudentId();
            if (tokenStudentId != studentId)
            {
                _logger.LogWarning("Token student ID ({TokenStudentId}) does not match requested student ID ({StudentId})", tokenStudentId, studentId);
                return Unauthorized(new { message = "You can only access your own exam history" });
            }

            // Validate parameters
            if (studentId <= 0 || schoolId <= 0 || levelId <= 0)
            {
                return BadRequest(new { message = "Student ID, School ID, and Level ID are required and must be greater than 0" });
            }

            // Get exam history
            var examHistory = await _appRepository.GetExamHistoryAsync(studentId, schoolId, levelId, classId);

            return Ok(examHistory);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("Unauthorized exam history access attempt: {Message}", ex.Message);
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting exam history for student {StudentId}", studentId);
            return StatusCode(500, new { message = "An error occurred while fetching exam history", error = ex.Message });
        }
    }

    [HttpGet("student-of-the-year")]
    [Authorize]
    public async Task<ActionResult<StudentOfTheYearResponseDto>> GetStudentOfTheYear(
        [FromQuery] int schoolId,
        [FromQuery] int levelId,
        [FromQuery] string? academicYear = null)
    {
        try
        {
            // Get student ID from token
            var studentId = GetCurrentStudentId();
            
            // Get student to get current academic year if not provided
            var student = await _appRepository.GetStudentByIdAsync(studentId);
            if (student == null)
            {
                return Unauthorized(new { message = "Student not found" });
            }

            // Use provided academicYear or current academic year
            var year = academicYear ?? student.CurrentAcademicYear;
            if (string.IsNullOrEmpty(year))
            {
                return BadRequest(new { message = "Academic year is required" });
            }

            var result = await _appRepository.GetStudentOfTheYearAsync(schoolId, levelId, year);

            // Format image URLs to full URLs
            var baseUrl = $"{Request.Scheme}://{Request.Host}{Request.PathBase}";
            foreach (var resultItem in result.Results)
            {
                if (!string.IsNullOrEmpty(resultItem.Image))
                {
                    // Check if it's already a full URL
                    if (!resultItem.Image.StartsWith("http://") && !resultItem.Image.StartsWith("https://"))
                    {
                        // It's a filename, construct the full URL
                        var sanitizedFileName = System.IO.Path.GetFileName(resultItem.Image);
                        resultItem.Image = $"{baseUrl}/Images/{sanitizedFileName}";
                    }
                }
                else
                {
                    // Set default image if no image provided
                    resultItem.Image = $"{baseUrl}/Images/default.png";
                }
            }

            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("Unauthorized student of the year access attempt: {Message}", ex.Message);
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching student of the year");
            return StatusCode(500, new { message = $"Error fetching student of the year: {ex.Message}" });
        }
    }

    [HttpGet("subjects")]
    [Authorize]
    public async Task<ActionResult<List<SubjectWithTeacherDto>>> GetSubjectsWithTeachers(
        [FromQuery] int studentId,
        [FromQuery] int schoolId,
        [FromQuery] int levelId,
        [FromQuery] int? classId = null)
    {
        try
        {
            // Validate token and ensure the student ID in token matches the requested student ID
            var tokenStudentId = GetCurrentStudentId();
            if (tokenStudentId != studentId)
            {
                _logger.LogWarning("Token student ID ({TokenStudentId}) does not match requested student ID ({StudentId})", tokenStudentId, studentId);
                return Unauthorized(new { message = "You can only access your own subjects" });
            }

            // Validate parameters
            if (studentId <= 0 || schoolId <= 0 || levelId <= 0)
            {
                return BadRequest(new { message = "Student ID, School ID, and Level ID are required and must be greater than 0" });
            }

            // Get subjects with teachers
            var subjects = await _appRepository.GetSubjectsWithTeachersAsync(schoolId, studentId, levelId, classId);

            return Ok(subjects);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("Unauthorized subjects access attempt: {Message}", ex.Message);
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting subjects for student {StudentId}", studentId);
            return StatusCode(500, new { message = "An error occurred while fetching subjects", error = ex.Message });
        }
    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<ActionResult> ChangePassword([FromBody] ChangePasswordRequest? request)
    {
        try
        {
            if (request == null)
            {
                return BadRequest(new { message = "Request body is required" });
            }

            if (string.IsNullOrEmpty(request.GenId) || 
                string.IsNullOrEmpty(request.CurrentPassword) || 
                string.IsNullOrEmpty(request.NewPassword))
            {
                return BadRequest(new { message = "GenId, current password, and new password are required" });
            }

            // Get student by GenId
            var student = await _appRepository.GetStudentByGenIdAsync(request.GenId);

            if (student == null)
            {
                _logger.LogWarning("Change password attempt with invalid Student ID: {GenId}", request.GenId);
                return Unauthorized(new { message = "Invalid Student ID" });
            }

            // Verify that the token student ID matches the student being updated
            var tokenStudentId = GetCurrentStudentId();
            if (tokenStudentId != student.Id)
            {
                _logger.LogWarning("Token student ID ({TokenStudentId}) does not match student ID ({StudentId})", tokenStudentId, student.Id);
                return Unauthorized(new { message = "You can only change your own password" });
            }

            // Verify current password - check if it's hashed or plain text
            bool passwordValid = false;
            if (!string.IsNullOrEmpty(student.Password))
            {
                // Try to verify as hashed password first
                passwordValid = Helper.VerifyPassword(request.CurrentPassword, student.Password);
                
                // If hashed verification fails, try plain text comparison (for backward compatibility)
                if (!passwordValid)
                {
                    passwordValid = student.Password == request.CurrentPassword;
                }
            }

            if (!passwordValid)
            {
                _logger.LogWarning("Change password attempt with invalid current password for GenId: {GenId}", request.GenId);
                return Unauthorized(new { message = "Current password is incorrect" });
            }

            // Check if student can change password based on Days setting in School
            if (student.School != null && student.School.Days.HasValue && student.School.Days.Value > 0)
            {
                if (student.PasswordUpdatedDate.HasValue)
                {
                    var daysSinceLastChange = (DateTime.UtcNow - student.PasswordUpdatedDate.Value).TotalDays;
                    var requiredDays = student.School.Days.Value;

                    if (daysSinceLastChange < requiredDays)
                    {
                        var remainingDays = Math.Ceiling(requiredDays - daysSinceLastChange);
                        var daysRemaining = (int)remainingDays;
                        _logger.LogWarning("Change password attempt too soon for GenId: {GenId}. Last changed {DaysSinceLastChange} days ago, {RequiredDays} days required. {RemainingDays} days remaining.", 
                            request.GenId, Math.Floor(daysSinceLastChange), requiredDays, remainingDays);
                        return BadRequest(new { 
                            message = $"You can only change your password after {requiredDays} days from your last password change. Please wait {daysRemaining} more day(s).",
                            remainingDays = daysRemaining,
                            requiredDays = requiredDays
                        });
                    }
                }
            }

            // Update password - hash it if needed
            // Check if current password is hashed, if so hash the new one too
            bool currentIsHashed = student.Password != request.CurrentPassword;
            student.Password = currentIsHashed ? Helper.HashPassword(request.NewPassword) : request.NewPassword;
            student.PasswordUpdatedDate = DateTime.UtcNow;

            // Update the student
            await _studentEnrollmentsRepository.UpdateAsync(student);

            _logger.LogInformation("Password changed successfully for GenId: {GenId}, StudentId: {StudentId}", request.GenId, student.Id);
            return Ok(new { message = "Password changed successfully" });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("Unauthorized change password attempt: {Message}", ex.Message);
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing password for GenId: {GenId}", request?.GenId ?? "unknown");
            return StatusCode(500, new { message = "An error occurred while changing password", error = ex.Message });
        }
    }

    private int GetCurrentStudentId()
    {
        // Get student ID from JWT claims (set during student login)
        var studentIdClaim = User.FindFirst("StudentId")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(studentIdClaim) || !int.TryParse(studentIdClaim, out int studentId))
        {
            throw new UnauthorizedAccessException("Student ID not found in token");
        }
        return studentId;
    }

    private string GenerateStudentJwtToken(StudentEnrollment student)
    {
        var jwtSettings = _configuration.GetSection("Jwt");
        var key = Encoding.UTF8.GetBytes(jwtSettings["Key"] ?? throw new InvalidOperationException("JWT Key not found"));
        var issuer = jwtSettings["Issuer"] ?? "SchoolMS";
        var audience = jwtSettings["Audience"] ?? "SchoolMS";
        var expiresInMinutes = int.Parse(jwtSettings["ExpiresInMinutes"] ?? "120");

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, student.Id.ToString()),
            new Claim("StudentId", student.Id.ToString()),
            new Claim(ClaimTypes.Name, student.Name),
            new Claim("GenId", student.GenId),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(expiresInMinutes),
            Issuer = issuer,
            Audience = audience,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature
            )
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    




}

