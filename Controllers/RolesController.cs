using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using schoolBE.Data.Interfaces;
using schoolBE.Models;
using schoolBE.ViewModels;

namespace schoolBE.Controllers;

[ApiController]
[Route("api/[controller]")]
[EnableCors(policyName: "CorePolicy")]
public class RolesController : ControllerBase
{
    private readonly IRoles _repository;

    public RolesController(IRoles repository)
    {
        _repository = repository;
    }

    [HttpGet]
    public async Task<ActionResult<List<Role>>> GetAll()
    {
        var roles = await _repository.GetAllAsync();
        return Ok(roles);
    }

    [HttpGet("vms")]
    public async Task<ActionResult<List<RoleVM>>> GetRolesVM()
    {
        var roles = await _repository.GetRolesAsync();
        return Ok(roles);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Role>> GetById(int id)
    {
        var role = await _repository.GetByIdAsync(id);
        if (role == null) return NotFound();
        return Ok(role);
    }

    [HttpGet("paged")]
    public async Task<ActionResult> GetPaged([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = null)
    {
        var result = await _repository.GetPagedAsync(page, pageSize, search);
        return Ok(new { data = result.data, totalCount = result.totalCount, page, pageSize });
    }

    [HttpPost]
    public async Task<ActionResult<Role>> Create(Role role)
    {
        var created = await _repository.AddAsync(role);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<Role>> Update(int id, Role role)
    {
        if (id != role.Id) return BadRequest();
        var updated = await _repository.UpdateAsync(role);
        return Ok(updated);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        var role = await _repository.GetByIdAsync(id);
        if (role == null) return NotFound();
        await _repository.DeleteAsync(role);
        return NoContent();
    }
}

