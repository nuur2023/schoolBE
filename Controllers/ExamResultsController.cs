using Microsoft.AspNetCore.Mvc;
using schoolBE.Data.Interfaces;
using schoolBE.Models;

namespace schoolBE.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ExamResultsController : ControllerBase
{
    private readonly IExamResults _repository;

    public ExamResultsController(IExamResults repository)
    {
        _repository = repository;
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
    public async Task<ActionResult<ExamResult>> Create(ExamResult result)
    {
        var created = await _repository.AddAsync(result);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ExamResult>> Update(int id, ExamResult result)
    {
        if (id != result.Id) return BadRequest();
        var updated = await _repository.UpdateAsync(result);
        return Ok(updated);
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

