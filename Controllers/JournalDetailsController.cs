using Microsoft.AspNetCore.Mvc;
using schoolBE.Data.Interfaces;
using schoolBE.Models;

namespace schoolBE.Controllers;

[ApiController]
[Route("api/[controller]")]
public class JournalDetailsController : ControllerBase
{
    private readonly IJournalDetails _repository;

    public JournalDetailsController(IJournalDetails repository)
    {
        _repository = repository;
    }

    [HttpGet]
    public async Task<ActionResult<List<JournalDetail>>> GetAll()
    {
        var details = await _repository.GetAllAsync();
        return Ok(details);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<JournalDetail>> GetById(int id)
    {
        var detail = await _repository.GetByIdAsync(id);
        if (detail == null) return NotFound();
        return Ok(detail);
    }

    [HttpGet("paged")]
    public async Task<ActionResult> GetPaged([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = null)
    {
        var result = await _repository.GetPagedAsync(page, pageSize, search);
        return Ok(new { data = result.data, totalCount = result.totalCount, page, pageSize });
    }

    [HttpPost]
    public async Task<ActionResult<JournalDetail>> Create(JournalDetail detail)
    {
        var created = await _repository.AddAsync(detail);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<JournalDetail>> Update(int id, JournalDetail detail)
    {
        if (id != detail.Id) return BadRequest();
        var updated = await _repository.UpdateAsync(detail);
        return Ok(updated);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        var detail = await _repository.GetByIdAsync(id);
        if (detail == null) return NotFound();
        await _repository.DeleteAsync(detail);
        return NoContent();
    }
}

