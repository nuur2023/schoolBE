using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using schoolBE.Data.Interfaces;
using schoolBE.Models;
using System.Security.Claims;

namespace schoolBE.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[EnableCors(policyName: "CorePolicy")]
public class StudentExitsController : ControllerBase
{
    private readonly IStudentExit _repository;

    public StudentExitsController(IStudentExit repository)
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
    [Authorize]
    public async Task<ActionResult<List<StudentExit>>> GetAll()
    {
        try
        {
            var exits = await _repository.GetAllAsync();
            return Ok(exits);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred while fetching student exits", error = ex.Message });
        }
    }

    [HttpGet("{id}")]
    [Authorize]
    public async Task<ActionResult<StudentExit>> GetById(int id)
    {
        try
        {
            var exit = await _repository.GetByIdAsync(id);
            if (exit == null) return NotFound();
            return Ok(exit);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"An error occurred while fetching student exit with ID {id}", error = ex.Message });
        }
    }

    [HttpGet("paged")]
    [Authorize]
    public async Task<ActionResult> GetPaged([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = null, [FromQuery] int? schoolId = null, [FromQuery] int? studentId = null)
    {
        try
        {
            var result = await _repository.GetPagedAsync(page, pageSize, search, schoolId, studentId);
            return Ok(new { data = result.data, totalCount = result.totalCount, page, pageSize });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred while fetching paged student exits", error = ex.Message });
        }
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<StudentExit>> Create([FromBody] StudentExit exit)
    {
        try
        {
            if (exit == null)
            {
                return BadRequest(new { message = "StudentExit data is required" });
            }
            exit.CreatorId = GetCurrentUserId();
            var created = await _repository.AddAsync(exit);
            var result = await _repository.GetByIdAsync(created.Id);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred while creating student exit", error = ex.Message });
        }
    }

    [HttpPut("{id}")]
    [Authorize]
    public async Task<ActionResult<StudentExit>> Update(int id, [FromBody] StudentExit exit)
    {
        try
        {
            if (id != exit.Id) return BadRequest();
            var updated = await _repository.UpdateAsync(exit);
            var result = await _repository.GetByIdAsync(updated.Id);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"An error occurred while updating student exit with ID {id}", error = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    [Authorize]
    public async Task<ActionResult> Delete(int id)
    {
        try
        {
            var exit = await _repository.GetByIdAsync(id);
            if (exit == null) return NotFound();
            await _repository.DeleteAsync(exit);
            return NoContent();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"An error occurred while deleting student exit with ID {id}", error = ex.Message });
        }
    }
}


























