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
public class StudentGraduationsController : ControllerBase
{
    private readonly IStudentGraduation _repository;

    public StudentGraduationsController(IStudentGraduation repository)
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
    public async Task<ActionResult<List<StudentGraduation>>> GetAll()
    {
        try
        {
            var graduations = await _repository.GetAllAsync();
            return Ok(graduations);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred while fetching student graduations", error = ex.Message });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<StudentGraduation>> GetById(int id)
    {
        try
        {
            var graduation = await _repository.GetByIdAsync(id);
            if (graduation == null) return NotFound();
            return Ok(graduation);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"An error occurred while fetching student graduation with ID {id}", error = ex.Message });
        }
    }

    [HttpGet("paged")]
    public async Task<ActionResult> GetPaged([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = null, [FromQuery] int? schoolId = null, [FromQuery] int? studentId = null)
    {
        try
        {
            var result = await _repository.GetPagedAsync(page, pageSize, search, schoolId, studentId);
            return Ok(new { data = result.data, totalCount = result.totalCount, page, pageSize });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred while fetching paged student graduations", error = ex.Message });
        }
    }

    [HttpPost]
    public async Task<ActionResult<StudentGraduation>> Create([FromBody] StudentGraduation graduation)
    {
        try
        {
            if (graduation == null)
            {
                return BadRequest(new { message = "Student graduation data is required" });
            }
            graduation.CreatorId = GetCurrentUserId();
            var created = await _repository.AddAsync(graduation);
            var result = await _repository.GetByIdAsync(created.Id);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred while creating student graduation", error = ex.Message });
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<StudentGraduation>> Update(int id, StudentGraduation graduation)
    {
        try
        {
            if (id != graduation.Id) return BadRequest();
            graduation.CreatorId = GetCurrentUserId(); // Ensure creator is updated if needed
            var updated = await _repository.UpdateAsync(graduation);
            var result = await _repository.GetByIdAsync(updated.Id);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"An error occurred while updating student graduation with ID {id}", error = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        try
        {
            var graduation = await _repository.GetByIdAsync(id);
            if (graduation == null) return NotFound();
            await _repository.DeleteAsync(graduation);
            return NoContent();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"An error occurred while deleting student graduation with ID {id}", error = ex.Message });
        }
    }
}


























