using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using schoolBE.Data.Interfaces;
using schoolBE.Models;
using schoolBE.ViewModels;
using System.Security.Claims;

namespace schoolBE.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[EnableCors(policyName: "CorePolicy")]
public class ExamSchedulesController : ControllerBase
{
    private readonly IExamSchedule _repository;

    public ExamSchedulesController(IExamSchedule repository)
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
    public async Task<ActionResult<List<ExamSchedule>>> GetAll()
    {
        var schedules = await _repository.GetAllAsync();
        return Ok(schedules);
    }

    // Static path segments MUST be registered before [HttpGet("{id}")] or "for-participants" is captured as id and fails int binding.
    [HttpGet("paged")]
    public async Task<ActionResult> GetPaged([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = null)
    {
        try
        {
            var result = await _repository.GetPagedAsync(page, pageSize, search);
            return Ok(new { data = result.data, totalCount = result.totalCount, page, pageSize });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred while fetching exam schedules", error = ex.Message });
        }
    }

    /// <summary>
    /// Active exam schedules for a school (Create Participants — labels only, not filtered by level/subjects).
    /// </summary>
    [HttpGet("for-participants")]
    public async Task<ActionResult<List<ExamSchedulePickerItemDto>>> GetForParticipants(
        [FromQuery] int schoolId)
    {
        if (schoolId <= 0)
            return BadRequest(new { message = "schoolId must be greater than zero." });

        try
        {
            var items = await _repository.GetActiveSchedulesPickerForSchoolAsync(schoolId);
            return Ok(items);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred while fetching exam schedules for participants", error = ex.Message });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ExamSchedule>> GetById(int id)
    {
        var schedule = await _repository.GetByIdAsync(id);
        if (schedule == null) return NotFound();
        return Ok(schedule);
    }

    [HttpPost]
    public async Task<ActionResult<ExamSchedule>> Create([FromBody] ExamSchedule schedule)
    {
        if (schedule == null)
        {
            return BadRequest(new { message = "ExamSchedule data is required" });
        }
        // CreatorId removed from ExamSchedule model
        var created = await _repository.AddAsync(schedule);
        // Reload with navigation properties
        var result = await _repository.GetByIdAsync(created.Id);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, result);
    }

    [HttpPost("bulk")]
    public async Task<ActionResult<List<ExamSchedule>>> CreateBulk([FromBody] List<ExamSchedule> schedules)
    {
        if (schedules == null || schedules.Count == 0)
        {
            return BadRequest(new { message = "ExamSchedules data is required" });
        }
        var currentUserId = GetCurrentUserId();
        var createdSchedules = new List<ExamSchedule>();
        foreach (var schedule in schedules)
        {
            // CreatorId removed from ExamSchedule model
            var created = await _repository.AddAsync(schedule);
            // Reload with navigation properties
            var result = await _repository.GetByIdAsync(created.Id);
            if (result != null)
            {
                createdSchedules.Add(result);
            }
        }
        return Ok(createdSchedules);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ExamSchedule>> Update(int id, ExamSchedule schedule)
    {
        if (id != schedule.Id) return BadRequest();
        var updated = await _repository.UpdateAsync(schedule);
        // Reload with navigation properties
        var result = await _repository.GetByIdAsync(updated.Id);
        return Ok(result);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        var schedule = await _repository.GetByIdAsync(id);
        if (schedule == null) return NotFound();
        await _repository.DeleteAsync(schedule);
        return NoContent();
    }
}
