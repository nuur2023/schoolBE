namespace schoolBE.Models;

public class Menu : BaseModel
{
    public string Name { get; set; } = string.Empty;
    public string Route { get; set; } = string.Empty;
    
    public ICollection<Submenu>? Submenus { get; set; }
}

