using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using schoolBE.Data.Interfaces;
using schoolBE.Models;
using System.Linq;

namespace schoolBE.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ExamResultsController : ControllerBase
{
    private readonly IExamResults _repository;
    private readonly IExamSchedule _examScheduleRepository;
    private readonly IExamParticipants _examParticipantsRepository;
    private readonly IStudentEnrollments _studentEnrollmentsRepository;

    public ExamResultsController(
        IExamResults repository,
        IExamSchedule examScheduleRepository,
        IExamParticipants examParticipantsRepository,
        IStudentEnrollments studentEnrollmentsRepository)
    {
        _repository = repository;
        _examScheduleRepository = examScheduleRepository;
        _examParticipantsRepository = examParticipantsRepository;
        _studentEnrollmentsRepository = studentEnrollmentsRepository;
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst("UserId")?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : 0;
    }

    [HttpGet]
    public async Task<ActionResult<List<ExamResult>>> GetAll()
    {
        var results = await _repository.GetAllAsync();
        return Ok(results);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ExamResult>> GetById(int id)
    {
        var result = await _repository.GetByIdAsync(id);
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpGet("paged")]
    public async Task<ActionResult> GetPaged([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = null)
    {
        var result = await _repository.GetPagedAsync(page, pageSize, search);
        return Ok(new { data = result.data, totalCount = result.totalCount, page, pageSize });
    }

    [HttpPost]
    public async Task<ActionResult<ExamResult>> Create([FromBody] ExamResult result)
    {
        result.CreatorId = GetCurrentUserId();
        
        // Calculate percentage
        if (result.FullMarks > 0)
        {
            result.Percentage = (result.Marks / result.FullMarks) * 100;
        }
        
        var created = await _repository.AddAsync(result);
        var createdWithNav = await _repository.GetByIdAsync(created.Id);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, createdWithNav);
    }

    [HttpPost("bulk")]
    public async Task<ActionResult<List<ExamResult>>> CreateBulk([FromBody] List<ExamResult> results)
    {
        if (results == null || results.Count == 0)
            return BadRequest("Results data is required");

        var userId = GetCurrentUserId();
        
        // Get the exam schedule ID from the first result (assuming all results are for the same exam schedule)
        var examScheduleId = results[0].ExamScheduleId;
        
        foreach (var result in results)
        {
            result.CreatorId = userId;
            
            // Calculate percentage
            if (result.FullMarks > 0)
            {
                result.Percentage = (result.Marks / result.FullMarks) * 100;
            }
        }
        
        var created = await _repository.CreateBulkAsync(results);
        
        // Get exam schedule with navigation properties to access level
        var examSchedule = await _examScheduleRepository.GetByIdAsync(examScheduleId);
        if (examSchedule == null)
        {
            return Ok(created); // Return results even if schedule not found
        }
        
        // Get the level ID from the exam schedule's LevelSubject
        var levelId = examSchedule.LevelSubject?.LevelId;
        if (!levelId.HasValue)
        {
            return Ok(created); // Return results if level not found
        }
        
        // Get all students enrolled in this level (across all classes)
        var allStudentsInLevel = await _studentEnrollmentsRepository.GetByStageLevelClassAsync(
            stageId: null,
            levelId: levelId.Value,
            classId: null,
            excludeStudentIds: null
        );
        
        // Filter to only active, non-graduated, non-left students
        var eligibleStudents = allStudentsInLevel
            .Where(s => s.IsActive && !s.IsGraduated && !s.IsLeft)
            .ToList();
        
        // Get all existing results for this exam schedule (including the ones just created)
        // Note: We use GetAllAsync and filter, but ideally this should be optimized with a repository method
        var allResults = await _repository.GetAllAsync();
        var resultsForThisSchedule = allResults
            .Where(r => r.ExamScheduleId == examScheduleId)
            .ToList();
        
        // Get distinct student IDs who have results for this exam schedule
        var studentsWithResults = resultsForThisSchedule
            .Select(r => r.StudentId)
            .Distinct()
            .ToHashSet();
        
        // Check if all eligible students have results
        var allStudentsHaveResults = eligibleStudents
            .All(student => studentsWithResults.Contains(student.Id));
        
        // Only deactivate exam schedule if all students in the level have results
        if (examSchedule.IsActive && allStudentsHaveResults && eligibleStudents.Count > 0)
        {
            examSchedule.IsActive = false;
            examSchedule.UpdatedAt = DateTime.UtcNow;
            await _examScheduleRepository.UpdateAsync(examSchedule);
        }
        
        // Update exam participants: Set ReceivedMarks to true for participants who received results
        var participants = await _examParticipantsRepository.GetByExamScheduleIdAsync(examScheduleId);
        var now = DateTime.UtcNow;
        
        // Only update participants whose students received results in this batch
        var studentIdsInBatch = created.Select(r => r.StudentId).ToHashSet();
        var participantsToUpdate = participants
            .Where(p => studentIdsInBatch.Contains(p.StudentId) && !p.ReceivedMarks)
            .ToList();
        
        foreach (var participant in participantsToUpdate)
        {
            participant.ReceivedMarks = true;
            participant.UpdatedAt = now;
            await _examParticipantsRepository.UpdateAsync(participant);
        }
        
        return Ok(created);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ExamResult>> Update(int id, [FromBody] ExamResult result)
    {
        if (id != result.Id) return BadRequest();
        
        var existing = await _repository.GetByIdAsync(id);
        if (existing == null) return NotFound();
        
        // Preserve CreatorId
        result.CreatorId = existing.CreatorId;
        
        // Calculate percentage
        if (result.FullMarks > 0)
        {
            result.Percentage = (result.Marks / result.FullMarks) * 100;
        }
        
        var updated = await _repository.UpdateAsync(result);
        var updatedWithNav = await _repository.GetByIdAsync(updated.Id);
        return Ok(updatedWithNav);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        var result = await _repository.GetByIdAsync(id);
        if (result == null) return NotFound();
        await _repository.DeleteAsync(result);
        return NoContent();
    }
}
