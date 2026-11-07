namespace HIS.Contracts.Auth;

public class CurrentUserDto
{
    public Guid AccountId { get; set; }

    public string Username { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    public int? MedicalWorkerId { get; set; }
}
