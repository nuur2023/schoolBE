using System.Text.Json.Serialization;

namespace schoolBE.ViewModels;

public class ChangePasswordRequest
{
    [JsonPropertyName("genId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public string GenId { get; set; } = string.Empty;

    [JsonPropertyName("currentPassword")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public string CurrentPassword { get; set; } = string.Empty;

    [JsonPropertyName("newPassword")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public string NewPassword { get; set; } = string.Empty;
}





