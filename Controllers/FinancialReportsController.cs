using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using schoolBE.Data;
using schoolBE.Data.Interfaces;
using schoolBE.Models;
using schoolBE.ViewModels;
using System.Security.Claims;

namespace schoolBE.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
[EnableCors(policyName: "CorePolicy")]
public class FinancialReportsController : ControllerBase
{
    private const int StudentFeesAccountId = 1;
    private const string StudentFeesAccountName = "Student fees";

    private readonly SchoolContext _context;
    private readonly IApp _appRepository;

    public FinancialReportsController(SchoolContext context, IApp appRepository)
    {
        _context = context;
        _appRepository = appRepository;
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
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

    private async Task<(int effectiveSchoolId, bool ok)> GetEffectiveSchoolIdAsync(int? schoolId)
    {
        var userSchoolId = await GetCurrentUserSchoolIdAsync();
        int effective = userSchoolId ?? schoolId ?? 0;
        if (effective <= 0)
            return (0, false);
        return (effective, true);
    }

    /// <summary>
    /// GET outstanding fees: students with name, stage, level, class, phone, balance.
    /// Optional filters: schoolId (for superadmin), stageId, levelId, classId, search (name/phone/genId).
    /// </summary>
    [HttpGet("outstanding-fees")]
    public async Task<ActionResult<List<OutstandingFeeRowDto>>> GetOutstandingFees(
        [FromQuery] int? schoolId,
        [FromQuery] int? stageId,
        [FromQuery] int? levelId,
        [FromQuery] int? classId,
        [FromQuery] string? search,
        [FromQuery] bool? balanceGreaterThanZeroOnly = true)
    {
        var userSchoolId = await GetCurrentUserSchoolIdAsync();
        int effectiveSchoolId = userSchoolId ?? schoolId ?? 0;
        if (effectiveSchoolId <= 0)
            return BadRequest(new { message = "School is required. Either log in as a school user or pass schoolId." });

        var query = _context.Set<StudentEnrollment>()
            .Include(s => s.Stage)
            .Include(s => s.Level)
            .Include(s => s.Class)
            .Where(s => s.SchoolId == effectiveSchoolId && s.IsActive && !s.IsGraduated && !s.IsLeft);

        if (stageId.HasValue)
            query = query.Where(s => s.StageId == stageId.Value);
        if (levelId.HasValue)
            query = query.Where(s => s.LevelId == levelId.Value);
        if (classId.HasValue)
            query = query.Where(s => s.ClassId == classId.Value);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            var studentIdsWithPhone = await _context.Set<Contact>()
                .Where(c => c.StudentId.HasValue && c.Phone.Contains(term))
                .Select(c => c.StudentId!.Value)
                .Distinct()
                .ToListAsync();
            query = query.Where(s =>
                s.Name.Contains(term) ||
                s.GenId.Contains(term) ||
                studentIdsWithPhone.Contains(s.Id));
        }

        var students = await query.OrderBy(s => s.Name).ToListAsync();
        var studentIds = students.Select(s => s.Id).ToList();

        var contacts = await _context.Set<Contact>()
            .Where(c => c.StudentId.HasValue && studentIds.Contains(c.StudentId.Value))
            .ToListAsync();
        var phoneByStudentId = contacts
            .GroupBy(c => c.StudentId!.Value)
            .ToDictionary(g => g.Key, g => g.First().Phone ?? string.Empty);

        var result = new List<OutstandingFeeRowDto>();
        foreach (var s in students)
        {
            var balance = await _appRepository.GetStudentBalanceAsync(s.Id);
            if (balanceGreaterThanZeroOnly == true && balance <= 0)
                continue;

            result.Add(new OutstandingFeeRowDto
            {
                StudentId = s.Id,
                Name = s.Name ?? string.Empty,
                GenId = s.GenId ?? string.Empty,
                Stage = s.Stage?.Name ?? string.Empty,
                Level = s.Level?.Name ?? string.Empty,
                Class = s.Class?.Name ?? string.Empty,
                Phone = phoneByStudentId.GetValueOrDefault(s.Id, string.Empty),
                Balance = balance
            });
        }

        return Ok(result);
    }

    [HttpGet("profit-loss")]
    public async Task<ActionResult<ProfitLossReportDto>> GetProfitLoss([FromQuery] int? schoolId)
    {
        var (effectiveSchoolId, ok) = await GetEffectiveSchoolIdAsync(schoolId);
        if (!ok)
            return BadRequest(new { message = "School is required." });

        var totalPaid = await _context.Set<Payment>()
            .Where(p => p.SchoolId == effectiveSchoolId)
            .SumAsync(p => (decimal?)p.Paid) ?? 0m;

        var expenses = await _context.Set<Expense>()
            .Where(e => e.SchoolId == effectiveSchoolId)
            .ToListAsync();
        var expenseByTitle = expenses
            .GroupBy(e => string.IsNullOrWhiteSpace(e.Title) ? "Expense" : e.Title.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(g => new ProfitLossLineDto { AccountName = g.Key, Amount = g.Sum(x => x.Amount) })
            .OrderBy(l => l.AccountName)
            .ToList();

        var payrollTotal = await _context.Set<PayRoll>()
            .Where(p => p.SchoolId == effectiveSchoolId)
            .SumAsync(p => (decimal?)p.NetPaid) ?? 0m;

        var expenseLines = new List<ProfitLossLineDto>(expenseByTitle);
        if (payrollTotal != 0)
            expenseLines.Add(new ProfitLossLineDto { AccountName = "Payroll", Amount = payrollTotal });

        var totalExpenses = expenseByTitle.Sum(l => l.Amount) + payrollTotal;

        return Ok(new ProfitLossReportDto
        {
            TotalRevenue = totalPaid,
            TotalExpenses = totalExpenses,
            NetProfitOrLoss = totalPaid - totalExpenses,
            RevenueLines =
            [
                new ProfitLossLineDto { AccountName = "Student fee collections", Amount = totalPaid }
            ],
            ExpenseLines = expenseLines,
        });
    }

    [HttpGet("balance-sheet")]
    public async Task<ActionResult<BalanceSheetReportDto>> GetBalanceSheet([FromQuery] int? schoolId)
    {
        var (effectiveSchoolId, ok) = await GetEffectiveSchoolIdAsync(schoolId);
        if (!ok)
            return BadRequest(new { message = "School is required." });

        var cash = await _context.Set<Payment>()
            .Where(p => p.SchoolId == effectiveSchoolId)
            .SumAsync(p => (decimal?)p.Paid) ?? 0m;

        var studentBalances = await _context.Set<Payment>()
            .Where(p => p.SchoolId == effectiveSchoolId && p.StudentId.HasValue)
            .GroupBy(p => p.StudentId!.Value)
            .Select(g => g.Sum(p => p.Charged - p.Paid))
            .ToListAsync();
        var receivables = studentBalances.Where(b => b > 0).Sum();

        var assets = new List<BalanceSheetLineDto>
        {
            new() { AccountName = "Cash", AccountType = "Asset", Balance = cash },
            new() { AccountName = "Receivables", AccountType = "Asset", Balance = receivables },
        };
        var liabilities = new List<BalanceSheetLineDto>();
        var totalAssets = cash + receivables;
        var totalLiabilities = 0m;
        var totalEquity = totalAssets - totalLiabilities;

        return Ok(new BalanceSheetReportDto
        {
            Assets = assets,
            Liabilities = liabilities,
            Equity =
            [
                new BalanceSheetLineDto { AccountName = "Equity", AccountType = "Equity", Balance = totalEquity }
            ],
            TotalAssets = totalAssets,
            TotalLiabilities = totalLiabilities,
            TotalEquity = totalEquity,
        });
    }

    [HttpGet("general-ledger")]
    public async Task<ActionResult<GeneralLedgerReportDto>> GetGeneralLedger(
        [FromQuery] int? schoolId,
        [FromQuery] int? schoolAccountId)
    {
        var (effectiveSchoolId, ok) = await GetEffectiveSchoolIdAsync(schoolId);
        if (!ok)
            return BadRequest(new { message = "School is required." });

        // schoolAccountId is ignored (no chart-of-accounts); synthetic Student fees account only.
        _ = schoolAccountId;

        var payments = await _context.Set<Payment>()
            .Where(p => p.SchoolId == effectiveSchoolId && (p.Charged > 0 || p.Paid > 0))
            .OrderBy(p => p.CreatedAt)
            .ThenBy(p => p.Id)
            .ToListAsync();

        var studentIds = payments.Where(p => p.StudentId.HasValue).Select(p => p.StudentId!.Value).Distinct().ToList();
        var students = await _context.Set<StudentEnrollment>()
            .Where(s => studentIds.Contains(s.Id))
            .ToListAsync();
        var studentById = students.ToDictionary(s => s.Id);

        decimal running = 0m;
        var lines = new List<GeneralLedgerLineDto>();
        foreach (var p in payments)
        {
            running += p.Charged - p.Paid;
            var genId = p.StudentId.HasValue && studentById.TryGetValue(p.StudentId.Value, out var st)
                ? (st.GenId ?? "")
                : "";
            var studentName = p.StudentId.HasValue && studentById.TryGetValue(p.StudentId.Value, out var st2)
                ? st2.Name
                : null;

            lines.Add(new GeneralLedgerLineDto
            {
                SchoolAccountId = StudentFeesAccountId,
                ReferenceNo = p.ReferenceNo ?? "",
                Description = string.IsNullOrWhiteSpace(studentName)
                    ? (p.Type ?? "Payment")
                    : $"{p.Type} — {studentName}",
                AccountName = StudentFeesAccountName,
                AccountType = "revenue",
                AccountNumber = genId,
                Date = p.CreatedAt,
                Debit = p.Charged,
                Credit = p.Paid,
                RunningBalance = running,
            });
        }

        var accounts = new List<GeneralLedgerAccountDto>
        {
            new() { Id = StudentFeesAccountId, Name = StudentFeesAccountName, AccountType = "revenue" }
        };

        return Ok(new GeneralLedgerReportDto { Accounts = accounts, Lines = lines });
    }

    [HttpGet("trial-balance")]
    public async Task<ActionResult<TrialBalanceReportDto>> GetTrialBalance([FromQuery] int? schoolId)
    {
        var (effectiveSchoolId, ok) = await GetEffectiveSchoolIdAsync(schoolId);
        if (!ok)
            return BadRequest(new { message = "School is required." });

        var totalCharged = await _context.Set<Payment>()
            .Where(p => p.SchoolId == effectiveSchoolId)
            .SumAsync(p => (decimal?)p.Charged) ?? 0m;
        var totalPaid = await _context.Set<Payment>()
            .Where(p => p.SchoolId == effectiveSchoolId)
            .SumAsync(p => (decimal?)p.Paid) ?? 0m;

        var lines = new List<TrialBalanceLineDto>
        {
            new()
            {
                AccountName = StudentFeesAccountName,
                AccountType = "revenue",
                TotalDebit = totalCharged,
                TotalCredit = totalPaid,
            }
        };

        var expenseGroups = await _context.Set<Expense>()
            .Where(e => e.SchoolId == effectiveSchoolId)
            .GroupBy(e => e.Title)
            .Select(g => new { Title = g.Key, Total = g.Sum(x => x.Amount) })
            .ToListAsync();

        foreach (var g in expenseGroups.OrderBy(x => x.Title))
        {
            lines.Add(new TrialBalanceLineDto
            {
                AccountName = string.IsNullOrWhiteSpace(g.Title) ? "Expense" : g.Title,
                AccountType = "expenses",
                TotalDebit = g.Total,
                TotalCredit = 0m,
            });
        }

        var payrollTotal = await _context.Set<PayRoll>()
            .Where(p => p.SchoolId == effectiveSchoolId)
            .SumAsync(p => (decimal?)p.NetPaid) ?? 0m;
        if (payrollTotal != 0)
        {
            lines.Add(new TrialBalanceLineDto
            {
                AccountName = "Payroll",
                AccountType = "expenses",
                TotalDebit = payrollTotal,
                TotalCredit = 0m,
            });
        }

        return Ok(new TrialBalanceReportDto
        {
            Lines = lines,
            TotalDebits = lines.Sum(l => l.TotalDebit),
            TotalCredits = lines.Sum(l => l.TotalCredit),
        });
    }

    [HttpGet("cash-flow")]
    public async Task<ActionResult<CashFlowReportDto>> GetCashFlow([FromQuery] int? schoolId)
    {
        var (effectiveSchoolId, ok) = await GetEffectiveSchoolIdAsync(schoolId);
        if (!ok)
            return BadRequest(new { message = "School is required." });

        var paidPayments = await _context.Set<Payment>()
            .Where(p => p.SchoolId == effectiveSchoolId && p.Paid > 0)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        var inflows = paidPayments.Select(p => new CashFlowLineDto
        {
            Description = string.IsNullOrWhiteSpace(p.PaymentMethod) ? "Fee collection" : $"Fee collection ({p.PaymentMethod})",
            ReferenceNo = p.ReferenceNo ?? "",
            Date = p.CreatedAt,
            Amount = p.Paid,
        }).ToList();

        var expenses = await _context.Set<Expense>()
            .Where(e => e.SchoolId == effectiveSchoolId && e.Amount > 0)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync();
        var payrolls = await _context.Set<PayRoll>()
            .Where(p => p.SchoolId == effectiveSchoolId && p.NetPaid > 0)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        var outflows = expenses.Select(e => new CashFlowLineDto
        {
            Description = string.IsNullOrWhiteSpace(e.Title) ? "Expense" : e.Title,
            ReferenceNo = e.Id.ToString(),
            Date = e.CreatedAt,
            Amount = e.Amount,
        }).Concat(payrolls.Select(p => new CashFlowLineDto
        {
            Description = string.IsNullOrWhiteSpace(p.PayPeriod) ? "Payroll" : $"Payroll ({p.PayPeriod})",
            ReferenceNo = p.Id.ToString(),
            Date = p.CreatedAt,
            Amount = p.NetPaid,
        }))
        .OrderByDescending(x => x.Date)
        .ToList();

        var cashIn = inflows.Sum(x => x.Amount);
        var cashOut = outflows.Sum(x => x.Amount);

        return Ok(new CashFlowReportDto
        {
            CashIn = cashIn,
            CashOut = cashOut,
            NetCashFlow = cashIn - cashOut,
            Inflows = inflows,
            Outflows = outflows,
        });
    }

    [HttpGet("student-fee-reports")]
    public async Task<ActionResult<StudentFeeReportDto>> GetStudentFeeReports(
        [FromQuery] int? schoolId,
        [FromQuery] int? stageId,
        [FromQuery] int? levelId,
        [FromQuery] int? classId,
        [FromQuery] string? search,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate)
    {
        var (effectiveSchoolId, ok) = await GetEffectiveSchoolIdAsync(schoolId);
        if (!ok)
            return BadRequest(new { message = "School is required." });

        var query = _context.Set<StudentEnrollment>()
            .Include(s => s.Stage)
            .Include(s => s.Level)
            .Include(s => s.Class)
            .Where(s => s.SchoolId == effectiveSchoolId && s.IsActive && !s.IsGraduated && !s.IsLeft);

        if (stageId.HasValue)
            query = query.Where(s => s.StageId == stageId.Value);
        if (levelId.HasValue)
            query = query.Where(s => s.LevelId == levelId.Value);
        if (classId.HasValue)
            query = query.Where(s => s.ClassId == classId.Value);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(s => s.Name.Contains(term) || s.GenId.Contains(term));
        }

        var students = await query.OrderBy(s => s.Name).ToListAsync();
        var studentIds = students.Select(s => s.Id).ToList();

        var paymentsQuery = _context.Set<Payment>()
            .Where(p => p.StudentId.HasValue && studentIds.Contains(p.StudentId.Value));

        if (fromDate.HasValue)
        {
            var from = fromDate.Value.Date;
            paymentsQuery = paymentsQuery.Where(p => p.CreatedAt >= from);
        }
        if (toDate.HasValue)
        {
            var toExclusive = toDate.Value.Date.AddDays(1);
            paymentsQuery = paymentsQuery.Where(p => p.CreatedAt < toExclusive);
        }

        var payments = await paymentsQuery.ToListAsync();
        var paymentsByStudent = payments
            .GroupBy(p => p.StudentId!.Value)
            .ToDictionary(g => g.Key, g => g.ToList());

        var result = new List<StudentFeeReportLineDto>();
        foreach (var s in students)
        {
            var studentPayments = paymentsByStudent.GetValueOrDefault(s.Id) ?? [];
            var totalCharged = studentPayments.Sum(p => p.Charged);
            var totalPaid = studentPayments.Sum(p => p.Paid);
            result.Add(new StudentFeeReportLineDto
            {
                StudentId = s.Id,
                StudentName = s.Name ?? "",
                GenId = s.GenId ?? "",
                Stage = s.Stage?.Name,
                Level = s.Level?.Name,
                Class = s.Class?.Name,
                TotalCharged = totalCharged,
                TotalPaid = totalPaid,
                Balance = totalCharged - totalPaid,
            });
        }

        return Ok(new StudentFeeReportDto { Students = result });
    }

    /// <summary>
    /// Student finance report: charged/paid summary plus charge &amp; payment lines.
    /// </summary>
    [HttpGet("student-finance")]
    public async Task<ActionResult<StudentFinanceReportDto>> GetStudentFinance(
        [FromQuery] int? schoolId,
        [FromQuery] int? stageId,
        [FromQuery] int? levelId,
        [FromQuery] int? classId,
        [FromQuery] string? search,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate)
    {
        var (effectiveSchoolId, ok) = await GetEffectiveSchoolIdAsync(schoolId);
        if (!ok)
            return BadRequest(new { message = "School is required." });

        var query = _context.Set<StudentEnrollment>()
            .Include(s => s.Stage)
            .Include(s => s.Level)
            .Include(s => s.Class)
            .Where(s => s.SchoolId == effectiveSchoolId && s.IsActive && !s.IsGraduated && !s.IsLeft);

        if (stageId.HasValue)
            query = query.Where(s => s.StageId == stageId.Value);
        if (levelId.HasValue)
            query = query.Where(s => s.LevelId == levelId.Value);
        if (classId.HasValue)
            query = query.Where(s => s.ClassId == classId.Value);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(s => s.Name.Contains(term) || s.GenId.Contains(term));
        }

        var students = await query.OrderBy(s => s.Name).ToListAsync();
        var studentIds = students.Select(s => s.Id).ToList();

        var paymentsQuery = _context.Set<Payment>()
            .Include(p => p.FeeCategories)
            .Where(p => p.StudentId.HasValue && studentIds.Contains(p.StudentId.Value));

        if (fromDate.HasValue)
        {
            var from = fromDate.Value.Date;
            paymentsQuery = paymentsQuery.Where(p => p.CreatedAt >= from);
        }
        if (toDate.HasValue)
        {
            var toExclusive = toDate.Value.Date.AddDays(1);
            paymentsQuery = paymentsQuery.Where(p => p.CreatedAt < toExclusive);
        }

        var payments = await paymentsQuery
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
        var paymentsByStudent = payments
            .GroupBy(p => p.StudentId!.Value)
            .ToDictionary(g => g.Key, g => g.ToList());

        var rows = new List<StudentFinanceRowDto>();
        foreach (var s in students)
        {
            var studentPayments = paymentsByStudent.GetValueOrDefault(s.Id) ?? [];
            var totalCharged = studentPayments.Sum(p => p.Charged);
            var totalPaid = studentPayments.Sum(p => p.Paid);
            rows.Add(new StudentFinanceRowDto
            {
                StudentId = s.Id,
                StudentName = s.Name ?? "",
                GenId = s.GenId ?? "",
                Stage = s.Stage?.Name ?? "",
                Level = s.Level?.Name ?? "",
                Class = s.Class?.Name ?? "",
                TotalCharged = totalCharged,
                TotalPaid = totalPaid,
                Balance = totalCharged - totalPaid,
                Transactions = studentPayments.Select(p =>
                {
                    var isCharge = string.Equals(p.Type, "charged", StringComparison.OrdinalIgnoreCase)
                        || (p.Charged > 0 && p.Paid <= 0);
                    return new StudentFinanceTxnDto
                    {
                        Id = p.Id,
                        Date = p.CreatedAt,
                        ReferenceNo = p.ReferenceNo ?? "",
                        Description = !string.IsNullOrWhiteSpace(p.FeeCategories?.Name)
                            ? p.FeeCategories!.Name!
                            : (isCharge ? "Charge" : "Payment"),
                        Type = isCharge ? "charge" : "payment",
                        Charged = p.Charged,
                        Paid = p.Paid,
                    };
                }).ToList(),
            });
        }

        return Ok(new StudentFinanceReportDto
        {
            Students = rows,
            Summary = new StudentFinanceSummaryDto
            {
                StudentCount = rows.Count,
                TotalCharged = rows.Sum(r => r.TotalCharged),
                TotalPaid = rows.Sum(r => r.TotalPaid),
                TotalBalance = rows.Sum(r => r.Balance),
            },
        });
    }

    [HttpGet("collection-reports")]
    public async Task<ActionResult<CollectionReportDto>> GetCollectionReports([FromQuery] int? schoolId)
    {
        var (effectiveSchoolId, ok) = await GetEffectiveSchoolIdAsync(schoolId);
        if (!ok)
            return BadRequest(new { message = "School is required." });

        var payments = await _context.Set<Payment>()
            .Where(p => p.SchoolId == effectiveSchoolId && p.Type == "paid" && p.Paid > 0)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        var studentIds = payments.Where(p => p.StudentId.HasValue).Select(p => p.StudentId!.Value).Distinct().ToList();
        var students = await _context.Set<StudentEnrollment>().Where(s => studentIds.Contains(s.Id)).ToListAsync();
        var studentById = students.ToDictionary(s => s.Id);
        var collections = payments.Select(p => new CollectionReportLineDto
        {
            Date = p.CreatedAt.ToString("yyyy-MM-dd"),
            ReferenceNo = p.ReferenceNo ?? "",
            StudentName = p.StudentId.HasValue && studentById.TryGetValue(p.StudentId.Value, out var st) ? st.Name : null,
            PaymentMethod = string.IsNullOrWhiteSpace(p.PaymentMethod) ? null : p.PaymentMethod,
            Amount = p.Paid,
        }).ToList();

        var total = collections.Sum(c => c.Amount);
        return Ok(new CollectionReportDto { Collections = collections, TotalCollected = total });
    }

    [HttpGet("financial-summary")]
    public async Task<ActionResult<FinancialSummaryDto>> GetFinancialSummary([FromQuery] int? schoolId)
    {
        var (effectiveSchoolId, ok) = await GetEffectiveSchoolIdAsync(schoolId);
        if (!ok)
            return BadRequest(new { message = "School is required." });

        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var monthlyRevenue = await _context.Set<Payment>()
            .Where(p => p.SchoolId == effectiveSchoolId && p.CreatedAt >= monthStart)
            .SumAsync(p => (decimal?)p.Paid) ?? 0m;

        var monthlyExpenseAmount = await _context.Set<Expense>()
            .Where(e => e.SchoolId == effectiveSchoolId && e.CreatedAt >= monthStart)
            .SumAsync(e => (decimal?)e.Amount) ?? 0m;
        var monthlyPayroll = await _context.Set<PayRoll>()
            .Where(p => p.SchoolId == effectiveSchoolId && p.CreatedAt >= monthStart)
            .SumAsync(p => (decimal?)p.NetPaid) ?? 0m;
        var monthlyExpenses = monthlyExpenseAmount + monthlyPayroll;

        var totalPaid = await _context.Set<Payment>()
            .Where(p => p.SchoolId == effectiveSchoolId)
            .SumAsync(p => (decimal?)p.Paid) ?? 0m;
        var totalExpense = await _context.Set<Expense>()
            .Where(e => e.SchoolId == effectiveSchoolId)
            .SumAsync(e => (decimal?)e.Amount) ?? 0m;
        var totalPayroll = await _context.Set<PayRoll>()
            .Where(p => p.SchoolId == effectiveSchoolId)
            .SumAsync(p => (decimal?)p.NetPaid) ?? 0m;
        var cashBalance = totalPaid - totalExpense - totalPayroll;

        return Ok(new FinancialSummaryDto
        {
            MonthlyRevenue = monthlyRevenue,
            MonthlyExpenses = monthlyExpenses,
            NetProfit = monthlyRevenue - monthlyExpenses,
            CashBalance = cashBalance
        });
    }
}
