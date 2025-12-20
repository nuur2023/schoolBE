namespace schoolBE.Models;

public class Submenu : BaseModel
{
    public int MenuId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Route { get; set; } = string.Empty;
    
    public Menu? Menu { get; set; }
}

