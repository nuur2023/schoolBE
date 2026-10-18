using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using schoolBE.Data.Interfaces;
using schoolBE.Models;
using schoolBE.Models.DTOs;
using System.Security.Claims;

namespace schoolBE.Controllers;

[ApiController]
[Route("api/attendances")]
[Authorize]
[EnableCors(policyName: "CorePolicy")]
public class AttendanceController : ControllerBase
{
    private readonly IAttendance _repository;

    public AttendanceController(IAttendance repository)
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
    public async Task<ActionResult<List<Attendance>>> GetAll()
    {
        var attendances = await _repository.GetAllAsync();
        return Ok(attendances);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Attendance>> GetById(int id)
    {
        var attendance = await _repository.GetByIdAsync(id);
        if (attendance == null) return NotFound();
        return Ok(attendance);
    }

    [HttpGet("paged")]
    public async Task<ActionResult> GetPaged([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = null)
    {
        var result = await _repository.GetPagedAsync(page, pageSize, search);
        return Ok(new { data = result.data, totalCount = result.totalCount, page, pageSize });
    }

    [HttpGet("by-filters")]
    public async Task<ActionResult<List<Attendance>>> GetByFilters(
        [FromQuery] int? schoolId = null,
        [FromQuery] int? stageId = null,
        [FromQuery] int? levelId = null,
        [FromQuery] int? classId = null,
        [FromQuery] int? studentId = null)
    {
        var attendances = await _repository.GetByFiltersAsync(schoolId, stageId, levelId, classId, studentId);
        return Ok(attendances);
    }

    [HttpPost]
    public async Task<ActionResult<Attendance>> Create(Attendance attendance)
    {
        attendance.CreatorId = GetCurrentUserId();
        var created = await _repository.AddAsync(attendance);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPost("bulk")]
    public async Task<ActionResult<List<Attendance>>> CreateBulk([FromBody] List<Attendance> attendances)
    {
        if (attendances == null || attendances.Count == 0)
        {
            return BadRequest(new { message = "Attendance data is required" });
        }

        var userId = GetCurrentUserId();
        foreach (var attendance in attendances)
        {
            attendance.CreatorId = userId;
            attendance.CreatedAt = DateTime.UtcNow;
        }

        var created = await _repository.CreateBulkAsync(attendances);
        return Ok(created);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<Attendance>> Update(int id, Attendance attendance)
    {
        if (id != attendance.Id) return BadRequest();
        var updated = await _repository.UpdateAsync(attendance);
        return Ok(updated);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        var attendance = await _repository.GetByIdAsync(id);
        if (attendance == null) return NotFound();
        await _repository.DeleteAsync(attendance);
        return NoContent();
    }

    [HttpGet("student/{studentId}")]
    public async Task<ActionResult<List<StudentAttendanceResponse>>> GetStudentAttendances(
        int studentId,
        [FromQuery] int schoolId,
        [FromQuery] int levelId,
        [FromQuery] int? months = 2)
    {
        var result = await _repository.GetStudentAttendancesAsync(studentId, schoolId, levelId, months);
        // Return as array to match mobile app structure
        return Ok(new List<StudentAttendanceResponse> { result });
    }
}

