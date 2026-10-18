using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using schoolBE.Data;
using schoolBE.Data.Interfaces;
using schoolBE.Models;
using schoolBE.Utils;
using schoolBE.ViewModels;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace schoolBE.Controllers;

[ApiController]
[Route("api/[controller]")]
[EnableCors(policyName: "CorePolicy")]
public class AuthController : ControllerBase
{
    private readonly IUsers _usersRepository;
    private readonly SchoolContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IUsers usersRepository, SchoolContext context, IConfiguration configuration, ILogger<AuthController> logger)
    {
        _usersRepository = usersRepository;
        _context = context;
        _configuration = configuration;
        _logger = logger;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request)
    {
        try
        {
            // Validate input
            if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
            {
                return BadRequest(new { message = "Email and password are required" });
            }

            // Get user by email
            var user = await _usersRepository.GetByEmailAsync(request.Email);
            if (user == null)
            {
                return Unauthorized(new { message = "Invalid email or password" });
            }

            // Check if user is active
            if (!user.IsActive)
            {
                return Unauthorized(new { message = "User account is inactive" });
            }

            // Verify password
            if (!Helper.VerifyPassword(request.Password, user.HashedPassword))
            {
                return Unauthorized(new { message = "Invalid email or password" });
            }

            // Generate JWT token
            var token = GenerateJwtToken(user);
            var expiresAt = DateTime.UtcNow.AddMinutes(120); // 2 hours

            // Create response
            var response = new LoginResponse
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                Token = token,
                ExpiresAt = expiresAt
            };

            // Set secure cookie with token
            // Using Vite proxy makes requests appear same-origin, so Lax works
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = false, // false for HTTP in development
                SameSite = SameSiteMode.Lax, // Lax works with same-origin requests (via proxy)
                Expires = expiresAt,
                Path = "/"
            };

            Response.Cookies.Append("authToken", token, cookieOptions);

            _logger.LogInformation("User {Email} logged in successfully", user.Email);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for email: {Email}", request.Email);
            return StatusCode(500, new { message = "An error occurred during login" });
        }
    }

    [HttpPost("logout")]
    [Authorize]
    public ActionResult Logout()
    {
        Response.Cookies.Delete("authToken");
        return Ok(new { message = "Logged out successfully" });
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult> GetCurrentUser()
    {
        try
        {
            var email = User.FindFirst(ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(email))
            {
                return Unauthorized();
            }

            var user = await _usersRepository.GetByEmailAsync(email);
            if (user == null)
            {
                return NotFound();
            }

            // Attach active school (if any) for admin UI
            var schoolUser = await _context.Set<SchoolUser>()
                .AsNoTracking()
                .Where(su => su.UsersId == user.Id && su.IsActive)
                .OrderByDescending(su => su.CreatedAt)
                .FirstOrDefaultAsync();

            School? school = null;
            if (schoolUser != null)
            {
                school = await _context.Set<School>()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.Id == schoolUser.SchoolId);
            }

            return Ok(new
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                IsActive = user.IsActive,
                SchoolId = schoolUser?.SchoolId,
                SchoolName = school?.Name,
                SchoolLogo = school?.Logo
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current user");
            return StatusCode(500, new { message = "An error occurred" });
        }
    }

    private string GenerateJwtToken(User user)
    {
        var jwtSettings = _configuration.GetSection("Jwt");
        var key = Encoding.UTF8.GetBytes(jwtSettings["Key"] ?? throw new InvalidOperationException("JWT Key not found"));
        var issuer = jwtSettings["Issuer"] ?? "SchoolMS";
        var audience = jwtSettings["Audience"] ?? "SchoolMS";
        var expiresInMinutes = int.Parse(jwtSettings["ExpiresInMinutes"] ?? "120");

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Name),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(expiresInMinutes),
            Issuer = issuer,
            Audience = audience,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature
            )
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}
