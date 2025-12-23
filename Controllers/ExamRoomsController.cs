using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using schoolBE.Data.Interfaces;
using schoolBE.Models;
using System.Security.Claims;

namespace schoolBE.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ExamRoomsController : ControllerBase
{
    private readonly IExamRooms _repository;

    public ExamRoomsController(IExamRooms repository)
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
    public async Task<ActionResult<List<ExamRoom>>> GetAll()
    {
        var rooms = await _repository.GetAllAsync();
        return Ok(rooms);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ExamRoom>> GetById(int id)
    {
        var room = await _repository.GetByIdAsync(id);
        if (room == null) return NotFound();
        return Ok(room);
    }

    [HttpGet("paged")]
    public async Task<ActionResult> GetPaged([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = null)
    {
        var result = await _repository.GetPagedAsync(page, pageSize, search);
        return Ok(new { data = result.data, totalCount = result.totalCount, page, pageSize });
    }

    [HttpPost]
    public async Task<ActionResult<ExamRoom>> Create([FromBody] ExamRoom room)
    {
        if (room == null) return BadRequest("Room data is required");

        room.CreatorId = GetCurrentUserId();
        room.CreatedAt = DateTime.UtcNow;
        room.UpdatedAt = DateTime.UtcNow;

        var created = await _repository.AddAsync(room);
        var result = await _repository.GetByIdAsync(created.Id);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, result);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ExamRoom>> Update(int id, [FromBody] ExamRoom room)
    {
        if (room == null) return BadRequest("Room data is required");

        var existing = await _repository.GetByIdAsync(id);
        if (existing == null) return NotFound();

        existing.Name = room.Name;
        existing.SchoolId = room.SchoolId;
        existing.Capacity = room.Capacity;
        existing.UpdatedAt = DateTime.UtcNow;

        var updated = await _repository.UpdateAsync(existing);
        var result = await _repository.GetByIdAsync(updated.Id);
        return Ok(result);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        var room = await _repository.GetByIdAsync(id);
        if (room == null) return NotFound();

        await _repository.DeleteAsync(room);
        return NoContent();
    }
}
