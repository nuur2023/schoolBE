namespace schoolBE.Models;

public class FeeCategories : BaseModel
{
    public string? Name { get; set; }
    public string? Type { get; set; }
    public int? SchoolId { get; set; }
    public List<Payment>? Payment { get; set; }
}




