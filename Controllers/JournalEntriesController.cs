using Microsoft.AspNetCore.Mvc;
using schoolBE.Data.Interfaces;
using schoolBE.Models;

namespace schoolBE.Controllers;

[ApiController]
[Route("api/[controller]")]
public class JournalEntriesController : ControllerBase
{
    private readonly IJournalEntry _repository;

    public JournalEntriesController(IJournalEntry repository)
    {
        _repository = repository;
    }

    [HttpGet]
    public async Task<ActionResult<List<JournalEntry>>> GetAll()
    {
        var entries = await _repository.GetAllAsync();
        return Ok(entries);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<JournalEntry>> GetById(int id)
    {
        var entry = await _repository.GetByIdAsync(id);
        if (entry == null) return NotFound();
        return Ok(entry);
    }

    [HttpGet("paged")]
    public async Task<ActionResult> GetPaged([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = null)
    {
        var result = await _repository.GetPagedAsync(page, pageSize, search);
        return Ok(new { data = result.data, totalCount = result.totalCount, page, pageSize });
    }

    [HttpPost]
    public async Task<ActionResult<JournalEntry>> Create(JournalEntry entry)
    {
        var created = await _repository.AddAsync(entry);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<JournalEntry>> Update(int id, JournalEntry entry)
    {
        if (id != entry.Id) return BadRequest();
        var updated = await _repository.UpdateAsync(entry);
        return Ok(updated);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        var entry = await _repository.GetByIdAsync(id);
        if (entry == null) return NotFound();
        await _repository.DeleteAsync(entry);
        return NoContent();
    }
}

