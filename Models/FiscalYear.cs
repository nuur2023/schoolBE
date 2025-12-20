namespace schoolBE.Models;

public class FiscalYear : BaseModel
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsClosed { get; set; }
    public int? SchoolId { get; set; }
    public int CreatorId { get; set; }
}

