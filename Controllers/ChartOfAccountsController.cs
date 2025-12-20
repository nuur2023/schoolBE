using Microsoft.AspNetCore.Mvc;
using schoolBE.Data.Interfaces;
using schoolBE.Models;

namespace schoolBE.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChartOfAccountsController : ControllerBase
{
    private readonly IChartOfAccount _repository;

    public ChartOfAccountsController(IChartOfAccount repository)
    {
        _repository = repository;
    }

    [HttpGet]
    public async Task<ActionResult<List<ChartOfAccount>>> GetAll()
    {
        var chartOfAccounts = await _repository.GetAllAsync();
        return Ok(chartOfAccounts);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ChartOfAccount>> GetById(int id)
    {
        var chartOfAccount = await _repository.GetByIdAsync(id);
        if (chartOfAccount == null) return NotFound();
        return Ok(chartOfAccount);
    }

    [HttpGet("paged")]
    public async Task<ActionResult> GetPaged([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = null)
    {
        var result = await _repository.GetPagedAsync(page, pageSize, search);
        return Ok(new { data = result.data, totalCount = result.totalCount, page, pageSize });
    }

    [HttpPost]
    public async Task<ActionResult<ChartOfAccount>> Create(ChartOfAccount chartOfAccount)
    {
        var created = await _repository.AddAsync(chartOfAccount);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ChartOfAccount>> Update(int id, ChartOfAccount chartOfAccount)
    {
        if (id != chartOfAccount.Id) return BadRequest();
        var updated = await _repository.UpdateAsync(chartOfAccount);
        return Ok(updated);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        var chartOfAccount = await _repository.GetByIdAsync(id);
        if (chartOfAccount == null) return NotFound();
        await _repository.DeleteAsync(chartOfAccount);
        return NoContent();
    }
}

