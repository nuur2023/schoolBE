namespace schoolBE.Models;

public class ChartOfAccount : BaseModel
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string NormalBalance { get; set; } = string.Empty;
}

