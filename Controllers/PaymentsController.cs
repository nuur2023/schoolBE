using Microsoft.AspNetCore.Mvc;
using schoolBE.Data.Interfaces;
using schoolBE.Models;

namespace schoolBE.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentsController : ControllerBase
{
    private readonly IPayment _repository;

    public PaymentsController(IPayment repository)
    {
        _repository = repository;
    }

    [HttpGet]
    public async Task<ActionResult<List<Payment>>> GetAll()
    {
        var payments = await _repository.GetAllAsync();
        return Ok(payments);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Payment>> GetById(int id)
    {
        var payment = await _repository.GetByIdAsync(id);
        if (payment == null) return NotFound();
        return Ok(payment);
    }

    [HttpGet("paged")]
    public async Task<ActionResult> GetPaged([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = null)
    {
        var result = await _repository.GetPagedAsync(page, pageSize, search);
        return Ok(new { data = result.data, totalCount = result.totalCount, page, pageSize });
    }

    [HttpPost]
    public async Task<ActionResult<Payment>> Create(Payment payment)
    {
        var created = await _repository.AddAsync(payment);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<Payment>> Update(int id, Payment payment)
    {
        if (id != payment.Id) return BadRequest();
        var updated = await _repository.UpdateAsync(payment);
        return Ok(updated);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        var payment = await _repository.GetByIdAsync(id);
        if (payment == null) return NotFound();
        await _repository.DeleteAsync(payment);
        return NoContent();
    }
}

