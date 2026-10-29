namespace HIS.Contracts.Accounts;

public class AccountPasswordDto
{
    public AccountDto Account { get; set; } = new();
    public string GeneratedPassword { get; set; } = string.Empty;
}
