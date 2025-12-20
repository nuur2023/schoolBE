using Microsoft.AspNetCore.Mvc;
using schoolBE.Data.Interfaces;
using schoolBE.Models;

namespace schoolBE.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AccountsController : ControllerBase
{
    private readonly IAccounts _repository;

    public AccountsController(IAccounts repository)
    {
        _repository = repository;
    }

    [HttpGet]
    public async Task<ActionResult<List<Account>>> GetAll()
    {
        var accounts = await _repository.GetAllAsync();
        return Ok(accounts);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Account>> GetById(int id)
    {
        var account = await _repository.GetByIdAsync(id);
        if (account == null) return NotFound();
        return Ok(account);
    }

    [HttpGet("paged")]
    public async Task<ActionResult> GetPaged([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = null)
    {
        var result = await _repository.GetPagedAsync(page, pageSize, search);
        return Ok(new { data = result.data, totalCount = result.totalCount, page, pageSize });
    }

    [HttpPost]
    public async Task<ActionResult<Account>> Create(Account account)
    {
        var created = await _repository.AddAsync(account);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<Account>> Update(int id, Account account)
    {
        if (id != account.Id) return BadRequest();
        var updated = await _repository.UpdateAsync(account);
        return Ok(updated);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        var account = await _repository.GetByIdAsync(id);
        if (account == null) return NotFound();
        await _repository.DeleteAsync(account);
        return NoContent();
    }
}

