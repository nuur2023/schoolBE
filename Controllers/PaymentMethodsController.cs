using Microsoft.AspNetCore.Mvc;
using schoolBE.Data.Interfaces;
using schoolBE.Models;

namespace schoolBE.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentMethodsController : ControllerBase
{
    private readonly IPaymentMethods _repository;

    public PaymentMethodsController(IPaymentMethods repository)
    {
        _repository = repository;
    }

    [HttpGet]
    public async Task<ActionResult<List<PaymentMethod>>> GetAll()
    {
        var methods = await _repository.GetAllAsync();
        return Ok(methods);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<PaymentMethod>> GetById(int id)
    {
        var method = await _repository.GetByIdAsync(id);
        if (method == null) return NotFound();
        return Ok(method);
    }

    [HttpGet("paged")]
    public async Task<ActionResult> GetPaged([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = null)
    {
        var result = await _repository.GetPagedAsync(page, pageSize, search);
        return Ok(new { data = result.data, totalCount = result.totalCount, page, pageSize });
    }

    [HttpPost]
    public async Task<ActionResult<PaymentMethod>> Create(PaymentMethod method)
    {
        var created = await _repository.AddAsync(method);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<PaymentMethod>> Update(int id, PaymentMethod method)
    {
        if (id != method.Id) return BadRequest();
        var updated = await _repository.UpdateAsync(method);
        return Ok(updated);
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(int id)
    {
        var method = await _repository.GetByIdAsync(id);
        if (method == null) return NotFound();
        await _repository.DeleteAsync(method);
        return NoContent();
    }

    [HttpPost("{id}/activate")]
    public async Task<ActionResult<PaymentMethod>> Activate(int id)
    {
        var method = await _repository.ActivateAsync(id);
        return Ok(method);
    }
}

