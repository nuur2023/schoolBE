namespace schoolBE.ViewModels;

public class SchoolDashboardDto
{
    public int TotalStudents { get; set; }
    public int TotalTeachers { get; set; }
    public int ActiveClasses { get; set; }
    public int UpcomingExams { get; set; }

    public int PaidStudents { get; set; }
    public decimal ChargedAmount { get; set; }
    public decimal PaidAmount { get; set; }

    public decimal TotalFeesCollected { get; set; }
    public decimal OutstandingFees { get; set; }
    public int UnpaidStudents { get; set; }

    public List<RecentActivityDto> RecentActivities { get; set; } = new();
    public List<RecentStudentDto> RecentlyRegisteredStudents { get; set; } = new();
    public int RecentlyRegisteredStudentsTotalCount { get; set; }
    public int RecentStudentsPage { get; set; }
    public int RecentStudentsPageSize { get; set; }
}

public class MonthlyPointDto
{
    public string Month { get; set; } = string.Empty;
    public decimal Income { get; set; }
    public decimal Expense { get; set; }
}

public class RecentActivityDto
{
    public string Type { get; set; } = string.Empty; // payment, expense, salary
    public string Title { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime Date { get; set; }
}

public class RecentStudentDto
{
    public string Name { get; set; } = string.Empty;
    public string? Image { get; set; }
    public string Stage { get; set; } = string.Empty;
    public string Level { get; set; } = string.Empty;
    public string Class { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public string Gender { get; set; } = string.Empty;
    public DateTime RegistrationDate { get; set; }
}

public class FinancialSummaryDto
{
    public decimal MonthlyRevenue { get; set; }
    public decimal MonthlyExpenses { get; set; }
    public decimal NetProfit { get; set; }
    public decimal CashBalance { get; set; }
}
