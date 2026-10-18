namespace schoolBE.ViewModels;

public class OutstandingFeeRowDto
{
    public int StudentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string GenId { get; set; } = string.Empty;
    public string Stage { get; set; } = string.Empty;
    public string Level { get; set; } = string.Empty;
    public string Class { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public decimal Balance { get; set; }
}
