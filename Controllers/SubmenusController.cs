using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using schoolBE.Data.Interfaces;
using schoolBE.Models;

namespace schoolBE.Controllers;

[ApiController]
[Route("api/[controller]")]
[EnableCors(policyName: "CorePolicy")]
public class SubmenusController : ControllerBase
{
    private readonly ISubmenus _repository;

    public SubmenusController(ISubmenus repository)
    {
        _repository = repository;
    }

    [HttpGet]
    public async Task<ActionResult<List<Submenu>>> GetAll()
    {
        var submenus = await _repository.GetAllAsync();
        return Ok(submenus);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Submenu>> GetById(int id)
    {
        var submenu = await _repository.GetByIdAsync(id);
        if (submenu == null) return NotFound();
        return Ok(submenu);
    }

    [HttpGet("paged")]
    public async Task<ActionResult> GetPaged([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = null)
    {
        var result = await _repository.GetPagedAsync(page, pageSize, search);
        return Ok(new { data = result.data, totalCount = result.totalCount, page, pageSize });
    }

    [HttpPost]
    public async Task<ActionResult<Submenu>> Create(Submenu submenu)
    {
        var created = await _repository.AddAsync(submenu);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<Submenu>> Update(int id, Submenu submenu)
    {
        if (id != submenu.Id) return BadRequest();
        var updated = await _repository.UpdateAsync(submenu);
        return Ok(updated);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        var submenu = await _repository.GetByIdAsync(id);
        if (submenu == null) return NotFound();
        await _repository.DeleteAsync(submenu);
        return NoContent();
    }
}

