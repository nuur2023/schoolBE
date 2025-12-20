using Microsoft.AspNetCore.Mvc;
using schoolBE.Data.Interfaces;
using schoolBE.Models;

namespace schoolBE.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FiscalYearsController : ControllerBase
{
    private readonly IFiscalYear _repository;

    public FiscalYearsController(IFiscalYear repository)
    {
        _repository = repository;
    }

    [HttpGet]
    public async Task<ActionResult<List<FiscalYear>>> GetAll()
    {
        var fiscalYears = await _repository.GetAllAsync();
        return Ok(fiscalYears);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<FiscalYear>> GetById(int id)
    {
        var fiscalYear = await _repository.GetByIdAsync(id);
        if (fiscalYear == null) return NotFound();
        return Ok(fiscalYear);
    }

    [HttpGet("paged")]
    public async Task<ActionResult> GetPaged([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = null)
    {
        var result = await _repository.GetPagedAsync(page, pageSize, search);
        return Ok(new { data = result.data, totalCount = result.totalCount, page, pageSize });
    }

    [HttpPost]
    public async Task<ActionResult<FiscalYear>> Create(FiscalYear fiscalYear)
    {
        var created = await _repository.AddAsync(fiscalYear);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<FiscalYear>> Update(int id, FiscalYear fiscalYear)
    {
        if (id != fiscalYear.Id) return BadRequest();
        var updated = await _repository.UpdateAsync(fiscalYear);
        return Ok(updated);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        var fiscalYear = await _repository.GetByIdAsync(id);
        if (fiscalYear == null) return NotFound();
        await _repository.DeleteAsync(fiscalYear);
        return NoContent();
    }
}

