using Microsoft.AspNetCore.Mvc;
using schoolBE.Data.Interfaces;
using schoolBE.Models;

namespace schoolBE.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SchoolsController : ControllerBase
{
    private readonly ISchools _repository;

    public SchoolsController(ISchools repository)
    {
        _repository = repository;
    }

    [HttpGet]
    public async Task<ActionResult<List<School>>> GetAll()
    {
        var schools = await _repository.GetAllAsync();
        return Ok(schools);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<School>> GetById(int id)
    {
        var school = await _repository.GetByIdAsync(id);
        if (school == null) return NotFound();
        return Ok(school);
    }

    [HttpGet("paged")]
    public async Task<ActionResult> GetPaged([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = null)
    {
        var result = await _repository.GetPagedAsync(page, pageSize, search);
        return Ok(new { data = result.data, totalCount = result.totalCount, page, pageSize });
    }

    [HttpPost]
    public async Task<ActionResult<School>> Create(School school)
    {
        var created = await _repository.AddAsync(school);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<School>> Update(int id, School school)
    {
        if (id != school.Id) return BadRequest();
        var updated = await _repository.UpdateAsync(school);
        return Ok(updated);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        var school = await _repository.GetByIdAsync(id);
        if (school == null) return NotFound();
        await _repository.DeleteAsync(school);
        return NoContent();
    }

    [HttpPost("{id}/activate")]
    public async Task<ActionResult<School>> Activate(int id)
    {
        var school = await _repository.ActivateAsync(id);
        return Ok(school);
    }
}

