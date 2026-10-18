using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using schoolBE.Data.Interfaces;
using schoolBE.Models;
using System.Security.Claims;

namespace schoolBE.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[EnableCors(policyName: "CorePolicy")]
public class PaymentMethodsController : ControllerBase
{
    private readonly IPaymentMethods _repository;

    public PaymentMethodsController(IPaymentMethods repository)
    {
        _repository = repository;
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
        {
            throw new UnauthorizedAccessException("User ID not found in token");
        }
        return userId;
    }

    [HttpGet]
    [Authorize]
    public async Task<ActionResult<List<PaymentMethod>>> GetAll()
    {
        try
        {
            var methods = await _repository.GetAllAsync();
            return Ok(methods);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred while fetching payment methods", error = ex.Message });
        }
    }

    [HttpGet("{id}")]
    [Authorize]
    public async Task<ActionResult<PaymentMethod>> GetById(int id)
    {
        try
        {
            var method = await _repository.GetByIdAsync(id);
            if (method == null) return NotFound();
            return Ok(method);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"An error occurred while fetching payment method with ID {id}", error = ex.Message });
        }
    }

    [HttpGet("paged")]
    [Authorize]
    public async Task<ActionResult> GetPaged([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? search = null)
    {
        try
        {
            var result = await _repository.GetPagedAsync(page, pageSize, search);
            return Ok(new { data = result.data, totalCount = result.totalCount, page, pageSize });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred while fetching paged payment methods", error = ex.Message });
        }
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<PaymentMethod>> Create([FromBody] PaymentMethod method)
    {
        try
        {
            if (method == null)
            {
                return BadRequest(new { message = "Payment method data is required" });
            }

            // Set CreatorId from the current authenticated user
            method.CreatorId = GetCurrentUserId();
            method.CreatedAt = DateTime.UtcNow;
            method.UpdatedAt = DateTime.UtcNow;

            var created = await _repository.AddAsync(method);
            var result = await _repository.GetByIdAsync(created.Id);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred while creating payment method", error = ex.Message });
        }
    }

    [HttpPut("{id}")]
    [Authorize]
    public async Task<ActionResult<PaymentMethod>> Update(int id, [FromBody] PaymentMethod method)
    {
        try
        {
            if (id != method.Id) return BadRequest();
            
            // Update the UpdatedAt timestamp
            method.UpdatedAt = DateTime.UtcNow;

            var updated = await _repository.UpdateAsync(method);
            var result = await _repository.GetByIdAsync(updated.Id);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"An error occurred while updating payment method with ID {id}", error = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    [Authorize]
    public async Task<ActionResult> Delete(int id)
    {
        try
        {
            var method = await _repository.GetByIdAsync(id);
            if (method == null) return NotFound();
            await _repository.DeleteAsync(method);
            return NoContent();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"An error occurred while deleting payment method with ID {id}", error = ex.Message });
        }
    }

    [HttpPost("{id}/activate")]
    [Authorize]
    public async Task<ActionResult<PaymentMethod>> Activate(int id)
    {
        try
        {
            var method = await _repository.ActivateAsync(id);
            return Ok(method);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"An error occurred while activating payment method with ID {id}", error = ex.Message });
        }
    }
}

