using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using schoolBE.Data;
using schoolBE.Models;
using schoolBE.ViewModels;
using System.Security.Claims;

namespace schoolBE.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[EnableCors(policyName: "CorePolicy")]
public class DashboardController : ControllerBase
{
    private readonly SchoolContext _context;

    public DashboardController(SchoolContext context)
    {
        _context = context;
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            throw new UnauthorizedAccessException("User ID not found in token");
        return userId;
    }

    private async Task<int?> GetCurrentUserSchoolIdAsync()
    {
        var userId = GetCurrentUserId();
        var schoolUser = await _context.Set<SchoolUser>()
            .FirstOrDefaultAsync(su => su.UsersId == userId && su.IsActive);
        return schoolUser?.SchoolId;
    }

    [HttpGet("school-summary")]
    public async Task<ActionResult<SchoolDashboardDto>> GetSchoolSummary(
        [FromQuery] int? schoolId,
        [FromQuery] string? recentStudentsSearch,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        var schoolIdFromToken = await GetCurrentUserSchoolIdAsync();
        var effectiveSchoolId = schoolIdFromToken ?? schoolId;
        if (!effectiveSchoolId.HasValue || effectiveSchoolId.Value <= 0)
            return BadRequest(new { message = "SchoolId is required for superadmin users." });

        var sid = effectiveSchoolId.Value;
        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1);
        var today = now.Date;
        var threeDaysAgo = today.AddDays(-3);
        page = page <= 0 ? 1 : page;
        pageSize = pageSize <= 0 ? 10 : pageSize;

        var totalStudents = await _context.Set<StudentEnrollment>()
            .CountAsync(s => s.SchoolId == sid && s.IsActive && !s.IsGraduated && !s.IsLeft);

        var teacherRoleIds = await _context.Set<Role>()
            .Where(r => r.Name.ToLower().Contains("teacher"))
            .Select(r => r.Id)
            .ToListAsync();
        var totalTeachers = await _context.Set<Person>()
            .CountAsync(p => p.SchoolId == sid && p.IsActive && teacherRoleIds.Contains(p.RoleId));

        var activeClasses = await _context.Set<Class>()
            .CountAsync(c => c.SchoolId == sid && c.IsActive);

        var upcomingExams = await _context.Set<ExamSchedule>()
            .CountAsync(e => e.SchoolId == sid && e.IsActive && e.StartDate.Date >= today);

        var monthlyPaidRows = await _context.Set<Payment>()
            .Where(p => p.SchoolId == sid && p.CreatedAt >= monthStart)
            .ToListAsync();
        var paidStudents = monthlyPaidRows
            .Where(p => p.Type == "paid" && p.Paid > 0 && p.StudentId.HasValue)
            .Select(p => p.StudentId!.Value)
            .Distinct()
            .Count();
        var chargedAmount = monthlyPaidRows.Sum(p => p.Charged - p.Paid); // decreases when payment posted
        var paidAmount = monthlyPaidRows.Sum(p => p.Paid);

        var totalFeesCollected = await _context.Set<Payment>()
            .Where(p => p.SchoolId == sid && p.Type == "paid")
            .SumAsync(p => (decimal?)p.Paid) ?? 0m;

        var studentBalances = await _context.Set<Payment>()
            .Where(p => p.SchoolId == sid && p.StudentId.HasValue)
            .GroupBy(p => p.StudentId!.Value)
            .Select(g => new
            {
                StudentId = g.Key,
                Balance = g.Sum(x => x.Charged - x.Paid)
            })
            .ToListAsync();

        // Only active enrolled students count toward outstanding / unpaid.
        var activeStudentIds = await _context.Set<StudentEnrollment>()
            .Where(s => s.SchoolId == sid && s.IsActive && !s.IsGraduated && !s.IsLeft)
            .Select(s => s.Id)
            .ToListAsync();
        var activeIdSet = activeStudentIds.ToHashSet();
        var activeBalances = studentBalances.Where(x => activeIdSet.Contains(x.StudentId)).ToList();
        var outstandingFees = activeBalances.Where(x => x.Balance > 0).Sum(x => x.Balance);
        var unpaidStudents = activeBalances.Count(x => x.Balance > 0);

        var recentPayments = await _context.Set<Payment>()
            .Where(p => p.SchoolId == sid && p.Type == "paid")
            .OrderByDescending(p => p.CreatedAt)
            .Take(5)
            .Select(p => new RecentActivityDto
            {
                Type = "payment",
                Title = $"Payment {p.ReferenceNo}",
                Amount = p.Paid,
                Date = p.CreatedAt
            })
            .ToListAsync();
        var recentExpenses = await _context.Set<Expense>()
            .Where(e => e.SchoolId == sid)
            .OrderByDescending(e => e.CreatedAt)
            .Take(5)
            .Select(e => new RecentActivityDto
            {
                Type = "expense",
                Title = e.Title,
                Amount = e.Amount,
                Date = e.CreatedAt
            })
            .ToListAsync();
        var recentSalaries = await _context.Set<PayRoll>()
            .Where(p => p.SchoolId == sid)
            .OrderByDescending(p => p.CreatedAt)
            .Take(5)
            .Select(p => new RecentActivityDto
            {
                Type = "salary",
                Title = $"Salary {p.PayPeriod}",
                Amount = p.NetPaid,
                Date = p.CreatedAt
            })
            .ToListAsync();
        var recentActivities = recentPayments
            .Concat(recentExpenses)
            .Concat(recentSalaries)
            .OrderByDescending(a => a.Date)
            .Take(10)
            .ToList();

        var recentStudentsQuery = _context.Set<StudentEnrollment>()
            .Where(s => s.SchoolId == sid && s.CreatedAt >= threeDaysAgo)
            .Include(s => s.Stage)
            .Include(s => s.Level)
            .Include(s => s.Class)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(recentStudentsSearch))
        {
            var term = recentStudentsSearch.Trim();
            recentStudentsQuery = recentStudentsQuery.Where(s =>
                s.Name.Contains(term) ||
                s.GenId.Contains(term) ||
                s.Gender.Contains(term) ||
                (s.Class != null && s.Class.Name.Contains(term)) ||
                (s.Level != null && s.Level.Name.Contains(term)) ||
                (s.Stage != null && s.Stage.Name.Contains(term)));
        }

        var recentStudentsTotalCount = await recentStudentsQuery.CountAsync();
        var recentStudentsRaw = await recentStudentsQuery
            .OrderByDescending(s => s.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var recentStudentIds = recentStudentsRaw.Select(s => s.Id).ToList();
        var recentStudentBalances = await _context.Set<Payment>()
            .Where(p => p.SchoolId == sid && p.StudentId.HasValue && recentStudentIds.Contains(p.StudentId.Value))
            .GroupBy(p => p.StudentId!.Value)
            .Select(g => new { StudentId = g.Key, Balance = g.Sum(x => x.Charged - x.Paid) })
            .ToListAsync();
        var recentBalanceByStudentId = recentStudentBalances.ToDictionary(x => x.StudentId, x => x.Balance);

        var recentStudents = recentStudentsRaw
            .Select(s => new RecentStudentDto
            {
                Name = s.Name,
                Image = s.Photo,
                Stage = s.Stage?.Name ?? string.Empty,
                Level = s.Level?.Name ?? string.Empty,
                Class = s.Class?.Name ?? string.Empty,
                Balance = recentBalanceByStudentId.GetValueOrDefault(s.Id, 0m),
                Gender = s.Gender,
                RegistrationDate = s.CreatedAt
            })
            .ToList();

        var result = new SchoolDashboardDto
        {
            TotalStudents = totalStudents,
            TotalTeachers = totalTeachers,
            ActiveClasses = activeClasses,
            UpcomingExams = upcomingExams,
            PaidStudents = paidStudents,
            ChargedAmount = chargedAmount,
            PaidAmount = paidAmount,
            TotalFeesCollected = totalFeesCollected,
            OutstandingFees = outstandingFees,
            UnpaidStudents = unpaidStudents,
            RecentActivities = recentActivities,
            RecentlyRegisteredStudents = recentStudents,
            RecentlyRegisteredStudentsTotalCount = recentStudentsTotalCount,
            RecentStudentsPage = page,
            RecentStudentsPageSize = pageSize
        };

        return Ok(result);
    }
}
