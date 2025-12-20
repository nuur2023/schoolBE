using Microsoft.AspNetCore.Mvc;
using schoolBE.Data.Interfaces;
using schoolBE.Models;

namespace schoolBE.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SchoolAccountsController : ControllerBase
{
    private readonly ISchoolAccounts _repository;

    public SchoolAccountsController(ISchoolAccounts repository)
    {
        _repository = repository;
    }

    [HttpGet]
    public async Task<ActionResult<List<SchoolAccount>>> GetAll()
    {
        var schoolAccounts = await _repository.GetAllAsync();
        return Ok(schoolAccounts);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<SchoolAccount>> GetById(int id)
    {
        var schoolAccount = await _repository.GetByIdAsync(id);
        if (schoolAccount == null) return NotFound();
        return Ok(schoolAccount);
    }

    [HttpGet("paged")]
    public async Task<ActionResult> GetPaged([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = null)
    {
        var result = await _repository.GetPagedAsync(page, pageSize, search);
        return Ok(new { data = result.data, totalCount = result.totalCount, page, pageSize });
    }

    [HttpPost]
    public async Task<ActionResult<SchoolAccount>> Create(SchoolAccount schoolAccount)
    {
        var created = await _repository.AddAsync(schoolAccount);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<SchoolAccount>> Update(int id, SchoolAccount schoolAccount)
    {
        if (id != schoolAccount.Id) return BadRequest();
        var updated = await _repository.UpdateAsync(schoolAccount);
        return Ok(updated);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        var schoolAccount = await _repository.GetByIdAsync(id);
        if (schoolAccount == null) return NotFound();
        await _repository.DeleteAsync(schoolAccount);
        return NoContent();
    }
}

