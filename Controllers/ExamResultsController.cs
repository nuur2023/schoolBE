using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using schoolBE.Data.Interfaces;
using schoolBE.Models;
using System.Linq;

namespace schoolBE.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[EnableCors(policyName: "CorePolicy")]
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
    public async Task<ActionResult> GetPaged(
        [FromQuery] int page = 1, 
        [FromQuery] int pageSize = 10, 
        [FromQuery] string? search = null,
        [FromQuery] int? schoolId = null,
        [FromQuery] int? stageId = null,
        [FromQuery] int? levelId = null,
        [FromQuery] int? classId = null,
        [FromQuery] int? examId = null,
        [FromQuery] string? publicationStatus = null)
    {
        try
        {
            var result = await _repository.GetPagedAsync(page, pageSize, search, schoolId, stageId, levelId, classId, examId, publicationStatus);
            return Ok(new { data = result.data, totalCount = result.totalCount, page, pageSize });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred while fetching exam results", error = ex.Message });
        }
    }

    [HttpPost]
    public async Task<ActionResult<ExamResult>> Create([FromBody] ExamResult result)
    {
        // CreatorId removed from ExamResult model
        
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

        // CreatorId removed from ExamResult model
        
        // Get the exam subject ID from the first result (assuming all results are for the same exam subject)
        var examSubjectId = results[0].ExamSubjectId;
        
        foreach (var result in results)
        {
            // Calculate percentage
            if (result.FullMarks > 0)
            {
                result.Percentage = (result.Marks / result.FullMarks) * 100;
            }
        }
        
        var created = await _repository.CreateBulkAsync(results);
        
        // Get exam subject with navigation properties to access level
        // Access ExamSubject through context since we don't have IExamSubject interface yet
        var examSubject = await _repository.GetByIdAsync(created[0].Id);
        if (examSubject?.ExamSubject == null)
        {
            return Ok(created); // Return results even if subject not found
        }
        
        // Get the level ID from the exam subject's LevelSubject
        var levelId = examSubject.ExamSubject.LevelSubject?.LevelId;
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
        
        // Get all existing results for this exam subject (including the ones just created)
        var allResults = await _repository.GetAllAsync();
        var resultsForThisSubject = allResults
            .Where(r => r.ExamSubjectId == examSubjectId)
            .ToList();
        
        // Get distinct student IDs who have results for this exam subject
        var studentsWithResults = resultsForThisSubject
            .Select(r => r.StudentId)
            .Distinct()
            .ToHashSet();
        
        // Check if all eligible students have results
        var allStudentsHaveResults = eligibleStudents
            .All(student => studentsWithResults.Contains(student.Id));
        
        // Get exam schedule from exam subject
        var examSchedule = examSubject.ExamSubject.ExamSchedule;
        if (examSchedule != null && allStudentsHaveResults && eligibleStudents.Count > 0)
        {
            // Note: IsActive is no longer on ExamSchedule, so we skip this check
            // The exam schedule completion logic may need to be rethought
        }
        
        // Update exam participants: Set ReceivedMarks to true for participants who received results
        // Get ExamScheduleId from ExamSubject
        var examScheduleId = examSubject.ExamSubject.ExamScheduleId;
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
        if (result == null)
        {
            return BadRequest(new { message = "Exam result data is required" });
        }

        if (id != result.Id)
        {
            return BadRequest(new { message = $"Route ID ({id}) does not match body ID ({result.Id})" });
        }
        
        var existing = await _repository.GetByIdAsync(id);
        if (existing == null)
        {
            return NotFound(new { message = $"Exam result with ID {id} not found" });
        }
        
        // Update the tracked entity's properties instead of creating a new entity
        existing.ExamSubjectId = result.ExamSubjectId;
        existing.StudentId = result.StudentId;
        existing.SchoolId = result.SchoolId;
        existing.AcademicYear = result.AcademicYear;
        existing.Date = result.Date;
        existing.Marks = result.Marks;
        existing.FullMarks = result.FullMarks;
        existing.PublicationStatus = result.PublicationStatus;
        // CreatorId removed from ExamResult model
        
        // Calculate percentage
        if (existing.FullMarks > 0)
        {
            existing.Percentage = (existing.Marks / existing.FullMarks) * 100;
        }
        else
        {
            return BadRequest(new { message = "Full marks must be greater than 0" });
        }
        
        try
        {
            // Update the tracked entity - it already has navigation properties loaded
            var updated = await _repository.UpdateAsync(existing);
            // Return the updated entity (it's already tracked with all navigation properties)
            return Ok(existing);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"Error updating exam result: {ex.Message}" });
        }
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        var result = await _repository.GetByIdAsync(id);
        if (result == null) return NotFound();
        await _repository.DeleteAsync(result);
        return NoContent();
    }

    [HttpPut("bulk-update-publication-status")]
    public async Task<ActionResult> BulkUpdatePublicationStatus(
        [FromBody] BulkUpdatePublicationStatusRequest request,
        [FromQuery] int? schoolId = null,
        [FromQuery] int? stageId = null,
        [FromQuery] int? levelId = null,
        [FromQuery] int? classId = null,
        [FromQuery] int? examId = null)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.PublicationStatus))
        {
            return BadRequest(new { message = "PublicationStatus is required" });
        }

        if (request.PublicationStatus != "Published" && request.PublicationStatus != "Draft" && request.PublicationStatus != "History" && request.PublicationStatus != "history")
        {
            return BadRequest(new { message = "PublicationStatus must be either 'Published', 'Draft', or 'History'" });
        }
        
        // Normalize status to proper case
        string normalizedStatus = request.PublicationStatus;
        if (normalizedStatus.Equals("history", StringComparison.OrdinalIgnoreCase))
        {
            normalizedStatus = "History";
        }
        else if (normalizedStatus.Equals("draft", StringComparison.OrdinalIgnoreCase))
        {
            normalizedStatus = "Draft";
        }
        else if (normalizedStatus.Equals("published", StringComparison.OrdinalIgnoreCase))
        {
            normalizedStatus = "Published";
        }

        // At least one filter must be provided (including studentIds)
        var hasStudentIds = request.StudentIds != null && request.StudentIds.Count > 0;
        if (!schoolId.HasValue && !stageId.HasValue && !levelId.HasValue && !classId.HasValue && !examId.HasValue && !hasStudentIds)
        {
            return BadRequest(new { message = "At least one filter (schoolId, stageId, levelId, classId, examId, or studentIds) must be provided" });
        }

        try
        {
            var updatedCount = await _repository.BulkUpdatePublicationStatusAsync(
                schoolId,
                stageId,
                levelId,
                classId,
                examId,
                normalizedStatus,
                request.CurrentPublicationStatus,
                request.StudentIds
            );

            return Ok(new { message = $"Successfully updated {updatedCount} exam result(s) to {normalizedStatus}", updatedCount });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"Error updating publication status: {ex.Message}" });
        }
    }
}

public class BulkUpdatePublicationStatusRequest
{
    public string PublicationStatus { get; set; } = string.Empty;
    /// <summary>When set, only rows with this current status are updated (e.g. "Published" when unpublishing).</summary>
    public string? CurrentPublicationStatus { get; set; }
    /// <summary>When set, only results for these student enrollment IDs are updated.</summary>
    public List<int>? StudentIds { get; set; }
}