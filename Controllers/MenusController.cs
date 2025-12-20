using Microsoft.AspNetCore.Mvc;
using schoolBE.Data.Interfaces;
using schoolBE.Models;

namespace schoolBE.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MenusController : ControllerBase
{
    private readonly IMenus _repository;

    public MenusController(IMenus repository)
    {
        _repository = repository;
    }

    [HttpGet]
    public async Task<ActionResult<List<Menu>>> GetAll()
    {
        var menus = await _repository.GetAllAsync();
        return Ok(menus);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Menu>> GetById(int id)
    {
        var menu = await _repository.GetByIdAsync(id);
        if (menu == null) return NotFound();
        return Ok(menu);
    }

    [HttpGet("paged")]
    public async Task<ActionResult> GetPaged([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = null)
    {
        var result = await _repository.GetPagedAsync(page, pageSize, search);
        return Ok(new { data = result.data, totalCount = result.totalCount, page, pageSize });
    }

    [HttpPost]
    public async Task<ActionResult<Menu>> Create(Menu menu)
    {
        var created = await _repository.AddAsync(menu);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<Menu>> Update(int id, Menu menu)
    {
        if (id != menu.Id) return BadRequest();
        var updated = await _repository.UpdateAsync(menu);
        return Ok(updated);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        var menu = await _repository.GetByIdAsync(id);
        if (menu == null) return NotFound();
        await _repository.DeleteAsync(menu);
        return NoContent();
    }
}

