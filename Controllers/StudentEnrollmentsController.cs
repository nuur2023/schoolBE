using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using schoolBE.Data.Interfaces;
using schoolBE.Models;
using System.Security.Claims;

namespace schoolBE.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class StudentEnrollmentsController : ControllerBase
{
    private readonly IStudentEnrollments _repository;

    public StudentEnrollmentsController(IStudentEnrollments repository)
    {
        _repository = repository;
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

    [HttpGet]
    public async Task<ActionResult<List<StudentEnrollment>>> GetAll()
    {
        var students = await _repository.GetAllAsync();
        return Ok(students);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<StudentEnrollment>> GetById(int id)
    {
        var student = await _repository.GetByIdAsync(id);
        if (student == null) return NotFound();
        return Ok(student);
    }

    [HttpGet("paged")]
    public async Task<ActionResult> GetPaged([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = null)
    {
        var result = await _repository.GetPagedAsync(page, pageSize, search);
        return Ok(new { data = result.data, totalCount = result.totalCount, page, pageSize });
    }

    [HttpPost]
    public async Task<ActionResult<StudentEnrollment>> Create([FromBody] StudentEnrollment student)
    {
        if (student == null)
        {
            return BadRequest(new { message = "Student data is required" });
        }
        student.CreatorId = GetCurrentUserId();
        
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
        
        // Update with GenId
        var updated = await _repository.UpdateAsync(created);
        
        // Reload with navigation properties
        var result = await _repository.GetByIdAsync(updated.Id);
        return CreatedAtAction(nameof(GetById), new { id = result!.Id }, result);
    }

    [HttpPut("{id}")]
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
        
        // Update photo if provided
        if (!string.IsNullOrEmpty(student.Photo))
        {
            existingStudent.Photo = student.Photo;
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
        return Ok(result);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        var student = await _repository.GetByIdAsync(id);
        if (student == null) return NotFound();
        await _repository.DeleteAsync(student);
        return NoContent();
    }

    [HttpPost("{id}/activate")]
    public async Task<ActionResult<StudentEnrollment>> Activate(int id)
    {
        var student = await _repository.ActivateAsync(id);
        return Ok(student);
    }
}

