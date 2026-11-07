namespace HIS.Contracts.Auth;

public class AdminSetupDto
{
    public string ManualKey { get; set; } = string.Empty;

    public string OtpAuthUri { get; set; } = string.Empty;
}