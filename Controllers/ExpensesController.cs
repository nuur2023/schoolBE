using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using schoolBE.Data.Interfaces;
using schoolBE.Models;

namespace schoolBE.Controllers;

[ApiController]
[Route("api/[controller]")]
[EnableCors(policyName: "CorePolicy")]
public class ExpensesController : ControllerBase
{
    private readonly IExpense _repository;

    public ExpensesController(IExpense repository)
    {
        _repository = repository;
    }

    [HttpGet]
    public async Task<ActionResult<List<Expense>>> GetAll()
    {
        var expenses = await _repository.GetAllAsync();
        return Ok(expenses);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Expense>> GetById(int id)
    {
        var expense = await _repository.GetByIdAsync(id);
        if (expense == null) return NotFound();
        return Ok(expense);
    }

    [HttpGet("paged")]
    public async Task<ActionResult> GetPaged([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = null)
    {
        var result = await _repository.GetPagedAsync(page, pageSize, search);
        return Ok(new { data = result.data, totalCount = result.totalCount, page, pageSize });
    }

    [HttpPost]
    public async Task<ActionResult<Expense>> Create(Expense expense)
    {
        var created = await _repository.AddAsync(expense);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<Expense>> Update(int id, Expense expense)
    {
        if (id != expense.Id) return BadRequest();
        var updated = await _repository.UpdateAsync(expense);
        return Ok(updated);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        var expense = await _repository.GetByIdAsync(id);
        if (expense == null) return NotFound();
        await _repository.DeleteAsync(expense);
        return NoContent();
    }
}

