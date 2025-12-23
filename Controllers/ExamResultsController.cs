using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using schoolBE.Data.Interfaces;
using schoolBE.Models;

namespace schoolBE.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ExamResultsController : ControllerBase
{
    private readonly IExamResults _repository;

    public ExamResultsController(IExamResults repository)
    {
        _repository = repository;
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
        var userId = GetCurrentUserId();
        
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
