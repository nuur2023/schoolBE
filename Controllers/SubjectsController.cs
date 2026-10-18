using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using schoolBE.Data.Interfaces;
using schoolBE.Models;

namespace schoolBE.Controllers;

[ApiController]
[Route("api/[controller]")]
[EnableCors(policyName: "CorePolicy")]
public class SubjectsController : ControllerBase
{
    private readonly ISubjects _repository;

    public SubjectsController(ISubjects repository)
    {
        _repository = repository;
    }

    [HttpGet]
    public async Task<ActionResult<List<Subject>>> GetAll()
    {
        var subjects = await _repository.GetAllAsync();
        return Ok(subjects);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Subject>> GetById(int id)
    {
        var subject = await _repository.GetByIdAsync(id);
        if (subject == null) return NotFound();
        return Ok(subject);
    }

    [HttpGet("paged")]
    public async Task<ActionResult> GetPaged([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = null)
    {
        var result = await _repository.GetPagedAsync(page, pageSize, search);
        return Ok(new { data = result.data, totalCount = result.totalCount, page, pageSize });
    }

    [HttpPost]
    public async Task<ActionResult<Subject>> Create(Subject subject)
    {
        var created = await _repository.AddAsync(subject);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPost("bulk")]
    public async Task<ActionResult<List<Subject>>> CreateBulk(List<Subject> subjects)
    {
        var createdSubjects = new List<Subject>();
        foreach (var subject in subjects)
        {
            var created = await _repository.AddAsync(subject);
            createdSubjects.Add(created);
        }
        return Ok(createdSubjects);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<Subject>> Update(int id, Subject subject)
    {
        if (id != subject.Id) return BadRequest();
        var updated = await _repository.UpdateAsync(subject);
        return Ok(updated);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        var subject = await _repository.GetByIdAsync(id);
        if (subject == null) return NotFound();
        await _repository.DeleteAsync(subject);
        return NoContent();
    }

    [HttpPost("{id}/activate")]
    public async Task<ActionResult<Subject>> Activate(int id)
    {
        var subject = await _repository.ActivateAsync(id);
        return Ok(subject);
    }
}

