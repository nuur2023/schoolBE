using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using schoolBE.Data.Interfaces;
using schoolBE.Models;
using System.Security.Claims;
using System.IO;

namespace schoolBE.Controllers;

[ApiController]
[Route("api/[controller]")]
[EnableCors(policyName: "CorePolicy")]
public class StudentEnrollmentsController : ControllerBase
{
    private readonly IStudentEnrollments _repository;
    private readonly IConfiguration _configuration;

    public StudentEnrollmentsController(IStudentEnrollments repository, IConfiguration configuration)
    {
        _repository = repository;
        _configuration = configuration;
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
        {
            throw new UnauthorizedAccessException("User ID not found in token");
        }
        return userId;
    }

    private void FormatPhotoUrl(StudentEnrollment student)
    {
        if (!string.IsNullOrEmpty(student.Photo))
        {
            // Check if it's already a full URL
            if (!student.Photo.StartsWith("http://") && !student.Photo.StartsWith("https://"))
            {
                // It's a filename, construct the full URL
                var sanitizedFileName = System.IO.Path.GetFileName(student.Photo);
                student.Photo = $"{Request.Scheme}://{Request.Host}{Request.PathBase}/Images/{sanitizedFileName}";
            }
        }
    }

    private void FormatPhotoUrls(List<StudentEnrollment> students)
    {
        foreach (var student in students)
        {
            FormatPhotoUrl(student);
        }
    }

    [HttpGet]
    [Authorize]
    public async Task<ActionResult<List<StudentEnrollment>>> GetAll()
    {
        var students = await _repository.GetAllAsync();
        FormatPhotoUrls(students);
        return Ok(students);
    }

    [HttpGet("{id}")]
    [Authorize]
    public async Task<ActionResult<StudentEnrollment>> GetById(int id)
    {
        var student = await _repository.GetByIdAsync(id);
        if (student == null) return NotFound();
        FormatPhotoUrl(student);
        return Ok(student);
    }

    [HttpGet("paged")]
    [Authorize]
    public async Task<ActionResult> GetPaged([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = null, [FromQuery] bool? isActive = null)
    {
        var result = await _repository.GetPagedAsync(page, pageSize, search, isActive);
        FormatPhotoUrls(result.data);
        return Ok(new { data = result.data, totalCount = result.totalCount, page, pageSize });
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<StudentEnrollment>> Create([FromBody] StudentEnrollment student)
    {
        if (student == null)
        {
            return BadRequest(new { message = "Student data is required" });
        }
        student.CreatorId = GetCurrentUserId();
        
        // Save photo temporarily if it's base64 (we'll process it after GenId is generated)
        string? tempPhotoData = null;
        if (!string.IsNullOrEmpty(student.Photo) && 
            (student.Photo.StartsWith("data:", StringComparison.OrdinalIgnoreCase) || 
             student.Photo.Contains("base64", StringComparison.OrdinalIgnoreCase)))
        {
            tempPhotoData = student.Photo;
            student.Photo = null; // Clear it temporarily
        }
        
        // Generate GenId: STD + studentId (will be set after save) + schoolId + 3 random letters + current year (2 digits)
        // We'll generate it after saving to get the student ID
        var created = await _repository.AddAsync(student);
        
        // Generate GenId now that we have the student ID
        var random = new Random();
        var randomLetters = new string(Enumerable.Range(0, 3)
            .Select(_ => (char)('A' + random.Next(26)))
            .ToArray());
        var currentYear = DateTime.Now.Year % 100; // Last 2 digits of year
        created.GenId = $"STD{created.Id}{created.SchoolId}{randomLetters}{currentYear:D2}";
        
        // Process photo if it was provided (now we have GenId)
        if (!string.IsNullOrEmpty(tempPhotoData))
        {
            try
            {
                var imagesPath = Path.Combine(Directory.GetCurrentDirectory(), "Images");
                if (!Directory.Exists(imagesPath))
                {
                    Directory.CreateDirectory(imagesPath);
                }

                // Extract base64 string and image format
                string base64String = tempPhotoData;
                string extension = "png"; // default
                
                if (base64String.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase))
                {
                    var parts = base64String.Split(',');
                    if (parts.Length == 2)
                    {
                        var header = parts[0];
                        if (header.Contains("jpeg") || header.Contains("jpg"))
                            extension = "jpg";
                        else if (header.Contains("png"))
                            extension = "png";
                        else if (header.Contains("gif"))
                            extension = "gif";
                        
                        base64String = parts[1];
                    }
                }

                // Generate filename using student's GenId
                var fileName = $"{created.GenId}.{extension}";
                var filePath = Path.Combine(imagesPath, fileName);

                // Decode and save the image
                var imageBytes = Convert.FromBase64String(base64String);
                await System.IO.File.WriteAllBytesAsync(filePath, imageBytes);

                // Store only the filename in database
                created.Photo = fileName;
            }
            catch
            {
                // If photo processing fails, set to default
                created.Photo = "default.png";
            }
        }
        else if (!string.IsNullOrEmpty(student.Photo))
        {
            // It's already a filename, just store it
            created.Photo = student.Photo;
        }
        
        // Update with GenId and Photo
        var updated = await _repository.UpdateAsync(created);
        
        // Reload with navigation properties
        var result = await _repository.GetByIdAsync(updated.Id);
        FormatPhotoUrl(result!);
        return CreatedAtAction(nameof(GetById), new { id = result!.Id }, result);
    }

    [HttpPut("{id}")]
    [Authorize]
    public async Task<ActionResult<StudentEnrollment>> Update(int id, [FromBody] StudentEnrollment student)
    {
        if (student == null)
        {
            return BadRequest(new { message = "Student data is required" });
        }
        if (id != student.Id) return BadRequest(new { message = "ID mismatch" });
        
        // Get existing student to preserve GenId and other fields if not provided
        var existingStudent = await _repository.GetByIdAsync(id);
        if (existingStudent == null) return NotFound();
        
        // Update properties on the existing tracked entity
        existingStudent.Name = student.Name;
        existingStudent.DOB = student.DOB;
        existingStudent.POB = student.POB;
        existingStudent.Gender = student.Gender;
        existingStudent.Address = student.Address;
        existingStudent.Mother = student.Mother;
        existingStudent.SchoolId = student.SchoolId;
        existingStudent.StageId = student.StageId;
        existingStudent.LevelId = student.LevelId;
        existingStudent.ClassId = student.ClassId;
        existingStudent.CurrentAcademicYear = student.CurrentAcademicYear;
        existingStudent.IsActive = student.IsActive;
        existingStudent.IsActiveInApp = student.IsActiveInApp;
        existingStudent.IsGraduated = student.IsGraduated;
        existingStudent.IsLeft = student.IsLeft;
        
        // Update GraduatedDate if provided
        if (student.GraduatedDate.HasValue)
        {
            existingStudent.GraduatedDate = student.GraduatedDate;
        }
        else if (student.IsGraduated && !existingStudent.IsGraduated)
        {
            // If marking as graduated for the first time, set the date to now
            existingStudent.GraduatedDate = DateTime.UtcNow;
        }
        
        // Update photo if provided
        if (!string.IsNullOrEmpty(student.Photo))
        {
            // Check if Photo is base64 data or a filename
            if (student.Photo.StartsWith("data:", StringComparison.OrdinalIgnoreCase) || 
                student.Photo.Contains("base64", StringComparison.OrdinalIgnoreCase))
            {
                // It's base64 data - decode and save as file
                try
                {
                    var imagesPath = Path.Combine(Directory.GetCurrentDirectory(), "Images");
                    if (!Directory.Exists(imagesPath))
                    {
                        Directory.CreateDirectory(imagesPath);
                    }

                    // Extract base64 string and image format
                    string base64String = student.Photo;
                    string extension = "png"; // default
                    
                    if (base64String.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase))
                    {
                        var parts = base64String.Split(',');
                        if (parts.Length == 2)
                        {
                            var header = parts[0];
                            if (header.Contains("jpeg") || header.Contains("jpg"))
                                extension = "jpg";
                            else if (header.Contains("png"))
                                extension = "png";
                            else if (header.Contains("gif"))
                                extension = "gif";
                            
                            base64String = parts[1];
                        }
                    }

                    // Generate filename using student's GenId
                    var fileName = $"{existingStudent.GenId}.{extension}";
                    var filePath = Path.Combine(imagesPath, fileName);

                    // Decode and save the image
                    var imageBytes = Convert.FromBase64String(base64String);
                    await System.IO.File.WriteAllBytesAsync(filePath, imageBytes);

                    // Delete old photo if it exists and is different
                    if (!string.IsNullOrEmpty(existingStudent.Photo) && 
                        existingStudent.Photo != fileName && 
                        !existingStudent.Photo.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
                    {
                        var oldFilePath = Path.Combine(imagesPath, existingStudent.Photo);
                        if (System.IO.File.Exists(oldFilePath))
                        {
                            try
                            {
                                System.IO.File.Delete(oldFilePath);
                            }
                            catch
                            {
                                // Ignore errors when deleting old file
                            }
                        }
                    }

                    // Store only the filename in database
                    existingStudent.Photo = fileName;
                }
                catch
                {
                    // Log error but don't fail the update
                    existingStudent.Photo = "default.png"; // Fallback to default
                }
            }
            else
            {
                // It's already a filename, just store it
                existingStudent.Photo = student.Photo;
            }
        }
        
        // Update password only if provided and not empty
        if (!string.IsNullOrEmpty(student.Password))
        {
            existingStudent.Password = student.Password;
            existingStudent.PasswordUpdatedDate = DateTime.UtcNow;
        }
        
        // GenId and CreatorId should not be changed on update
        // They are already set on existingStudent
        
        var updated = await _repository.UpdateAsync(existingStudent);
        // Reload with navigation properties
        var result = await _repository.GetByIdAsync(updated.Id);
        FormatPhotoUrl(result!);
        return Ok(result);
    }

    [HttpDelete("{id}")]
    [Authorize]
    public async Task<ActionResult> Delete(int id)
    {
        var student = await _repository.GetByIdAsync(id);
        if (student == null) return NotFound();
        await _repository.DeleteAsync(student);
        return NoContent();
    }

    [HttpPost("{id}/activate")]
    [Authorize]
    public async Task<ActionResult<StudentEnrollment>> Activate(int id)
    {
        var student = await _repository.ActivateAsync(id);
        FormatPhotoUrl(student);
        return Ok(student);
    }

    [HttpGet("by-filters")]
    [Authorize]
    public async Task<ActionResult<List<StudentEnrollment>>> GetByStageLevelClass(
        [FromQuery] int? stageId = null,
        [FromQuery] int? levelId = null,
        [FromQuery] int? classId = null,
        [FromQuery] string? excludeStudentIds = null)
    {
        List<int>? excludeIds = null;
        if (!string.IsNullOrEmpty(excludeStudentIds))
        {
            excludeIds = excludeStudentIds.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(id => int.TryParse(id.Trim(), out int parsedId) ? parsedId : (int?)null)
                .Where(id => id.HasValue)
                .Select(id => id!.Value)
                .ToList();
        }
        var students = await _repository.GetByStageLevelClassAsync(stageId, levelId, classId, excludeIds);
        FormatPhotoUrls(students);
        return Ok(students);
    }
}

