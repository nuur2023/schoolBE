using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using schoolBE.Data.Interfaces;
using schoolBE.Models;
using System.Security.Claims;

namespace schoolBE.Controllers;

[ApiController]
[Route("api/examparticipants")]
[Authorize]
public class ExamParticipantsController : ControllerBase
{
    private readonly IExamParticipants _repository;

    public ExamParticipantsController(IExamParticipants repository)
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
    public async Task<ActionResult<List<ExamParticipants>>> GetAll()
    {
        var participants = await _repository.GetAllAsync();
        return Ok(participants);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ExamParticipants>> GetById(int id)
    {
        var participant = await _repository.GetByIdAsync(id);
        if (participant == null) return NotFound();
        return Ok(participant);
    }

    [HttpGet("paged")]
    public async Task<ActionResult> GetPaged([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = null, [FromQuery] int? examScheduleId = null)
    {
        var result = await _repository.GetPagedAsync(page, pageSize, search);
        
        // Filter by examScheduleId if provided
        if (examScheduleId.HasValue)
        {
            result.data = result.data.Where(p => p.ExamScheduleId == examScheduleId.Value).ToList();
            result.totalCount = result.data.Count;
        }
        
        return Ok(new { data = result.data, totalCount = result.totalCount, page, pageSize });
    }

    [HttpPost]
    public async Task<ActionResult<ExamParticipants>> Create([FromBody] ExamParticipants participant)
    {
        if (participant == null) return BadRequest("Participant data is required");

        participant.CreatorId = GetCurrentUserId();
        participant.CreatedAt = DateTime.UtcNow;
        participant.UpdatedAt = DateTime.UtcNow;
        
        if (string.IsNullOrEmpty(participant.Status))
        {
            participant.Status = "Scheduled";
        }

        var created = await _repository.AddAsync(participant);
        var result = await _repository.GetByIdAsync(created.Id);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, result);
    }

    [HttpPost("bulk")]
    public async Task<ActionResult<List<ExamParticipants>>> CreateBulk([FromBody] List<ExamParticipants> participants)
    {
        if (participants == null || participants.Count == 0) return BadRequest("Participants data is required");

        var currentUserId = GetCurrentUserId();
        var now = DateTime.UtcNow;

        foreach (var participant in participants)
        {
            participant.CreatorId = currentUserId;
            participant.CreatedAt = now;
            participant.UpdatedAt = now;
            
            if (string.IsNullOrEmpty(participant.Status))
            {
                participant.Status = "Scheduled";
            }
        }

        var created = new List<ExamParticipants>();
        foreach (var participant in participants)
        {
            var result = await _repository.AddAsync(participant);
            created.Add(result);
        }

        return Ok(created);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ExamParticipants>> Update(int id, [FromBody] ExamParticipants participant)
    {
        if (participant == null) return BadRequest("Participant data is required");

        var existing = await _repository.GetByIdAsync(id);
        if (existing == null) return NotFound();

        existing.ExamScheduleId = participant.ExamScheduleId;
        existing.StudentId = participant.StudentId;
        existing.ClassId = participant.ClassId;
        existing.LevelId = participant.LevelId;
        existing.RoomId = participant.RoomId;
        existing.SeatNumber = participant.SeatNumber;
        existing.Status = participant.Status;
        existing.StatusNote = participant.StatusNote;
        existing.StatusUpdatedAt = DateTime.UtcNow;
        existing.UpdatedAt = DateTime.UtcNow;

        var updated = await _repository.UpdateAsync(existing);
        var result = await _repository.GetByIdAsync(updated.Id);
        return Ok(result);
    }

    [HttpPut("bulk-status")]
    public async Task<ActionResult<List<ExamParticipants>>> UpdateBulkStatus([FromBody] BulkStatusUpdateRequest request)
    {
        if (request == null || request.ParticipantIds == null || request.ParticipantIds.Count == 0)
            return BadRequest("Participant IDs and status are required");

        var updated = new List<ExamParticipants>();
        var now = DateTime.UtcNow;

        foreach (var id in request.ParticipantIds)
        {
            var participant = await _repository.GetByIdAsync(id);
            if (participant != null)
            {
                participant.Status = request.Status;
                participant.StatusNote = request.StatusNote;
                participant.StatusUpdatedAt = now;
                participant.UpdatedAt = now;
                
                var result = await _repository.UpdateAsync(participant);
                updated.Add(result);
            }
        }

        return Ok(updated);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        var participant = await _repository.GetByIdAsync(id);
        if (participant == null) return NotFound();

        await _repository.DeleteAsync(participant);
        return NoContent();
    }

    [HttpGet("exam-schedule/{examScheduleId}")]
    public async Task<ActionResult<List<ExamParticipants>>> GetByExamScheduleId(int examScheduleId)
    {
        var participants = await _repository.GetByExamScheduleIdAsync(examScheduleId);
        return Ok(participants);
    }

    [HttpGet("exam-schedule/{examScheduleId}/student-ids")]
    public async Task<ActionResult<List<int>>> GetParticipatedStudentIds(int examScheduleId)
    {
        var studentIds = await _repository.GetParticipatedStudentIdsAsync(examScheduleId);
        return Ok(studentIds);
    }
}

public class BulkStatusUpdateRequest
{
    public List<int> ParticipantIds { get; set; } = new();
    public string Status { get; set; } = string.Empty;
    public string? StatusNote { get; set; }
}

