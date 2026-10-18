namespace schoolBE.ViewModels;

public class ProfitLossReportDto
{
    public decimal TotalRevenue { get; set; }
    public decimal TotalExpenses { get; set; }
    public decimal NetProfitOrLoss { get; set; }
    public List<ProfitLossLineDto> RevenueLines { get; set; } = new();
    public List<ProfitLossLineDto> ExpenseLines { get; set; } = new();
}

public class ProfitLossLineDto
{
    public string AccountName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}

public class BalanceSheetReportDto
{
    public decimal TotalAssets { get; set; }
    public decimal TotalLiabilities { get; set; }
    public decimal TotalEquity { get; set; }
    public List<BalanceSheetLineDto> Assets { get; set; } = new();
    public List<BalanceSheetLineDto> Liabilities { get; set; } = new();
    public List<BalanceSheetLineDto> Equity { get; set; } = new();
}

public class BalanceSheetLineDto
{
    public string AccountName { get; set; } = string.Empty;
    public string AccountType { get; set; } = string.Empty;
    public decimal Balance { get; set; }
}

public class GeneralLedgerLineDto
{
    /// <summary>Synthetic account id (always 1 = Student fees). Kept for FE grouping.</summary>
    public int SchoolAccountId { get; set; }
    public string ReferenceNo { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string AccountName { get; set; } = string.Empty;
    public string AccountType { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public decimal Debit { get; set; }
    public decimal Credit { get; set; }
    public decimal RunningBalance { get; set; }
}

public class GeneralLedgerAccountDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string AccountType { get; set; } = string.Empty;
}

public class GeneralLedgerReportDto
{
    public List<GeneralLedgerAccountDto> Accounts { get; set; } = new();
    public List<GeneralLedgerLineDto> Lines { get; set; } = new();
}

public class TrialBalanceLineDto
{
    public string AccountName { get; set; } = string.Empty;
    public string AccountType { get; set; } = string.Empty;
    public decimal TotalDebit { get; set; }
    public decimal TotalCredit { get; set; }
}

public class TrialBalanceReportDto
{
    public List<TrialBalanceLineDto> Lines { get; set; } = new();
    public decimal TotalDebits { get; set; }
    public decimal TotalCredits { get; set; }
}

public class CashFlowReportDto
{
    public decimal CashIn { get; set; }
    public decimal CashOut { get; set; }
    public decimal NetCashFlow { get; set; }
    public List<CashFlowLineDto> Inflows { get; set; } = new();
    public List<CashFlowLineDto> Outflows { get; set; } = new();
}

public class CashFlowLineDto
{
    public string Description { get; set; } = string.Empty;
    public string ReferenceNo { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public decimal Amount { get; set; }
}

public class StudentFeeReportLineDto
{
    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string GenId { get; set; } = string.Empty;
    public string? Stage { get; set; }
    public string? Level { get; set; }
    public string? Class { get; set; }
    public decimal TotalCharged { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal Balance { get; set; }
}

public class StudentFeeReportDto
{
    public List<StudentFeeReportLineDto> Students { get; set; } = new();
}

public class StudentFinanceTxnDto
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public string ReferenceNo { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // charge | payment
    public decimal Charged { get; set; }
    public decimal Paid { get; set; }
}

public class StudentFinanceRowDto
{
    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public string GenId { get; set; } = string.Empty;
    public string Stage { get; set; } = string.Empty;
    public string Level { get; set; } = string.Empty;
    public string Class { get; set; } = string.Empty;
    public decimal TotalCharged { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal Balance { get; set; }
    public List<StudentFinanceTxnDto> Transactions { get; set; } = new();
}

public class StudentFinanceSummaryDto
{
    public int StudentCount { get; set; }
    public decimal TotalCharged { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal TotalBalance { get; set; }
}

public class StudentFinanceReportDto
{
    public List<StudentFinanceRowDto> Students { get; set; } = new();
    public StudentFinanceSummaryDto Summary { get; set; } = new();
}

public class CollectionReportLineDto
{
    public string Date { get; set; } = string.Empty;
    public string ReferenceNo { get; set; } = string.Empty;
    public string? StudentName { get; set; }
    public string? PaymentMethod { get; set; }
    public decimal Amount { get; set; }
}

public class CollectionReportDto
{
    public List<CollectionReportLineDto> Collections { get; set; } = new();
    public decimal TotalCollected { get; set; }
}
