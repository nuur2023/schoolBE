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
public class FeeCategoriesController : ControllerBase
{
    private readonly IFeeCategories _repository;

    public FeeCategoriesController(IFeeCategories repository)
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
    public async Task<ActionResult<List<FeeCategories>>> GetAll()
    {
        try
        {
            var categories = await _repository.GetAllAsync();
            return Ok(categories);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred while fetching fee categories", error = ex.Message });
        }
    }

    [HttpGet("{id}")]
    [Authorize]
    public async Task<ActionResult<FeeCategories>> GetById(int id)
    {
        try
        {
            var category = await _repository.GetByIdAsync(id);
            if (category == null) return NotFound();
            return Ok(category);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"An error occurred while fetching fee category with ID {id}", error = ex.Message });
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
            return StatusCode(500, new { message = "An error occurred while fetching paged fee categories", error = ex.Message });
        }
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<FeeCategories>> Create([FromBody] FeeCategories category)
    {
        try
        {
            if (category == null)
            {
                return BadRequest(new { message = "Fee category data is required" });
            }

            // Set timestamps
            category.CreatedAt = DateTime.UtcNow;
            category.UpdatedAt = DateTime.UtcNow;

            var created = await _repository.AddAsync(category);
            var result = await _repository.GetByIdAsync(created.Id);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An error occurred while creating fee category", error = ex.Message });
        }
    }

    [HttpPut("{id}")]
    [Authorize]
    public async Task<ActionResult<FeeCategories>> Update(int id, [FromBody] FeeCategories category)
    {
        try
        {
            if (id != category.Id) return BadRequest();
            
            // Update the UpdatedAt timestamp
            category.UpdatedAt = DateTime.UtcNow;

            var updated = await _repository.UpdateAsync(category);
            var result = await _repository.GetByIdAsync(updated.Id);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"An error occurred while updating fee category with ID {id}", error = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    [Authorize]
    public async Task<ActionResult> Delete(int id)
    {
        try
        {
            var category = await _repository.GetByIdAsync(id);
            if (category == null) return NotFound();
            await _repository.DeleteAsync(category);
            return NoContent();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = $"An error occurred while deleting fee category with ID {id}", error = ex.Message });
        }
    }
}

















