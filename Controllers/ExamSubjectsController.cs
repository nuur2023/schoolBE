using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using schoolBE.Data.Interfaces;
using schoolBE.Models;

namespace schoolBE.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[EnableCors(policyName: "CorePolicy")]
public class ExamSubjectsController : ControllerBase
{
    private readonly IExamSubject _repository;

    public ExamSubjectsController(IExamSubject repository)
    {
        _repository = repository;
    }

    [HttpGet]
    public async Task<ActionResult<List<ExamSubject>>> GetAll()
    {
        var subjects = await _repository.GetAllAsync();
        return Ok(subjects);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ExamSubject>> GetById(int id)
    {
        var subject = await _repository.GetByIdAsync(id);
        if (subject == null) return NotFound();
        return Ok(subject);
    }

    [HttpGet("paged")]
    public async Task<ActionResult> GetPaged([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = null, [FromQuery] int? examScheduleId = null, [FromQuery] DateTime? examDate = null)
    {
        try
        {
            var result = await _repository.GetPagedAsync(page, pageSize, search, examScheduleId, examDate);
            return Ok(new { data = result.data, totalCount = result.totalCount, page, pageSize });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred while fetching exam subjects", error = ex.Message });
        }
    }

    [HttpPost]
    public async Task<ActionResult<ExamSubject>> Create([FromBody] ExamSubject subject)
    {
        if (subject == null)
        {
            return BadRequest(new { message = "ExamSubject data is required" });
        }
        var created = await _repository.AddAsync(subject);
        var result = await _repository.GetByIdAsync(created.Id);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, result);
    }

    [HttpPost("bulk")]
    public async Task<ActionResult<List<ExamSubject>>> CreateBulk([FromBody] List<ExamSubject> subjects)
    {
        if (subjects == null || subjects.Count == 0)
        {
            return BadRequest(new { message = "ExamSubjects data is required" });
        }
        var createdSubjects = new List<ExamSubject>();
        foreach (var subject in subjects)
        {
            var created = await _repository.AddAsync(subject);
            var result = await _repository.GetByIdAsync(created.Id);
            if (result != null)
            {
                createdSubjects.Add(result);
            }
        }
        return Ok(createdSubjects);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ExamSubject>> Update(int id, ExamSubject subject)
    {
        if (id != subject.Id) return BadRequest();
        var updated = await _repository.UpdateAsync(subject);
        var result = await _repository.GetByIdAsync(updated.Id);
        return Ok(result);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        var subject = await _repository.GetByIdAsync(id);
        if (subject == null) return NotFound();
        await _repository.DeleteAsync(subject);
        return NoContent();
    }
}

