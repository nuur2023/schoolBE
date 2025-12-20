namespace schoolBE.Models;

public class YearEndClosingEntry : BaseModel
{
    public int FiscalYearId { get; set; }
    public int SchoolId { get; set; }
    public decimal TotalProfit { get; set; }
}

