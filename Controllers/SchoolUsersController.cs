using Microsoft.AspNetCore.Mvc;
using schoolBE.Data.Interfaces;
using schoolBE.Models;

namespace schoolBE.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SchoolUsersController : ControllerBase
{
    private readonly ISchoolUsers _repository;

    public SchoolUsersController(ISchoolUsers repository)
    {
        _repository = repository;
    }

    [HttpGet]
    public async Task<ActionResult<List<SchoolUser>>> GetAll()
    {
        var schoolUsers = await _repository.GetAllAsync();
        return Ok(schoolUsers);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<SchoolUser>> GetById(int id)
    {
        var schoolUser = await _repository.GetByIdAsync(id);
        if (schoolUser == null) return NotFound();
        return Ok(schoolUser);
    }

    [HttpGet("paged")]
    public async Task<ActionResult> GetPaged([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = null)
    {
        var result = await _repository.GetPagedAsync(page, pageSize, search);
        return Ok(new { data = result.data, totalCount = result.totalCount, page, pageSize });
    }

    [HttpPost]
    public async Task<ActionResult<SchoolUser>> Create(SchoolUser schoolUser)
    {
        var created = await _repository.AddAsync(schoolUser);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<SchoolUser>> Update(int id, SchoolUser schoolUser)
    {
        if (id != schoolUser.Id) return BadRequest();
        var updated = await _repository.UpdateAsync(schoolUser);
        return Ok(updated);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        var schoolUser = await _repository.GetByIdAsync(id);
        if (schoolUser == null) return NotFound();
        await _repository.DeleteAsync(schoolUser);
        return NoContent();
    }

    [HttpPost("{id}/activate")]
    public async Task<ActionResult<SchoolUser>> Activate(int id)
    {
        var schoolUser = await _repository.ActivateAsync(id);
        return Ok(schoolUser);
    }
}

