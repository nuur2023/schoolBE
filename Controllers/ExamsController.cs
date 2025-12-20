using Microsoft.AspNetCore.Mvc;
using schoolBE.Data.Interfaces;
using schoolBE.Models;

namespace schoolBE.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ExamsController : ControllerBase
{
    private readonly IExams _repository;

    public ExamsController(IExams repository)
    {
        _repository = repository;
    }

    [HttpGet]
    public async Task<ActionResult<List<Exam>>> GetAll()
    {
        var exams = await _repository.GetAllAsync();
        return Ok(exams);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Exam>> GetById(int id)
    {
        var exam = await _repository.GetByIdAsync(id);
        if (exam == null) return NotFound();
        return Ok(exam);
    }

    [HttpGet("paged")]
    public async Task<ActionResult> GetPaged([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = null)
    {
        var result = await _repository.GetPagedAsync(page, pageSize, search);
        return Ok(new { data = result.data, totalCount = result.totalCount, page, pageSize });
    }

    [HttpPost]
    public async Task<ActionResult<Exam>> Create(Exam exam)
    {
        var created = await _repository.AddAsync(exam);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<Exam>> Update(int id, Exam exam)
    {
        if (id != exam.Id) return BadRequest();
        var updated = await _repository.UpdateAsync(exam);
        return Ok(updated);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        var exam = await _repository.GetByIdAsync(id);
        if (exam == null) return NotFound();
        await _repository.DeleteAsync(exam);
        return NoContent();
    }

    [HttpPost("{id}/activate")]
    public async Task<ActionResult<Exam>> Activate(int id)
    {
        var exam = await _repository.ActivateAsync(id);
        return Ok(exam);
    }
}

