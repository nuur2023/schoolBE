using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using schoolBE.Data.Interfaces;
using schoolBE.Models;

namespace schoolBE.Controllers;

[ApiController]
[Route("api/[controller]")]
[EnableCors(policyName: "CorePolicy")]
public class PayRollsController : ControllerBase
{
    private readonly IPayRoll _repository;

    public PayRollsController(IPayRoll repository)
    {
        _repository = repository;
    }

    [HttpGet]
    public async Task<ActionResult<List<PayRoll>>> GetAll()
    {
        var payRolls = await _repository.GetAllAsync();
        return Ok(payRolls);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<PayRoll>> GetById(int id)
    {
        var payRoll = await _repository.GetByIdAsync(id);
        if (payRoll == null) return NotFound();
        return Ok(payRoll);
    }

    [HttpGet("paged")]
    public async Task<ActionResult> GetPaged([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = null)
    {
        var result = await _repository.GetPagedAsync(page, pageSize, search);
        return Ok(new { data = result.data, totalCount = result.totalCount, page, pageSize });
    }

    [HttpPost]
    public async Task<ActionResult<PayRoll>> Create(PayRoll payRoll)
    {
        var created = await _repository.AddAsync(payRoll);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<PayRoll>> Update(int id, PayRoll payRoll)
    {
        if (id != payRoll.Id) return BadRequest();
        var updated = await _repository.UpdateAsync(payRoll);
        return Ok(updated);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        var payRoll = await _repository.GetByIdAsync(id);
        if (payRoll == null) return NotFound();
        await _repository.DeleteAsync(payRoll);
        return NoContent();
    }
}

