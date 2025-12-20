using Microsoft.AspNetCore.Mvc;
using schoolBE.Data.Interfaces;
using schoolBE.Models;

namespace schoolBE.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LevelsController : ControllerBase
{
    private readonly ILevels _repository;

    public LevelsController(ILevels repository)
    {
        _repository = repository;
    }

    [HttpGet]
    public async Task<ActionResult<List<Level>>> GetAll()
    {
        var levels = await _repository.GetAllAsync();
        return Ok(levels);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Level>> GetById(int id)
    {
        var level = await _repository.GetByIdAsync(id);
        if (level == null) return NotFound();
        return Ok(level);
    }

    [HttpGet("paged")]
    public async Task<ActionResult> GetPaged([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = null)
    {
        var result = await _repository.GetPagedAsync(page, pageSize, search);
        return Ok(new { data = result.data, totalCount = result.totalCount, page, pageSize });
    }

    [HttpPost]
    public async Task<ActionResult<Level>> Create(Level level)
    {
        var created = await _repository.AddAsync(level);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPost("bulk")]
    public async Task<ActionResult<List<Level>>> CreateBulk(List<Level> levels)
    {
        var createdLevels = new List<Level>();
        foreach (var level in levels)
        {
            var created = await _repository.AddAsync(level);
            createdLevels.Add(created);
        }
        return Ok(createdLevels);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<Level>> Update(int id, Level level)
    {
        if (id != level.Id) return BadRequest();
        var updated = await _repository.UpdateAsync(level);
        return Ok(updated);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        var level = await _repository.GetByIdAsync(id);
        if (level == null) return NotFound();
        await _repository.DeleteAsync(level);
        return NoContent();
    }

    [HttpPost("{id}/activate")]
    public async Task<ActionResult<Level>> Activate(int id)
    {
        var level = await _repository.ActivateAsync(id);
        return Ok(level);
    }
}

