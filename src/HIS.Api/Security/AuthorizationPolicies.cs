namespace HIS.Api.Security;

public static class AuthorizationPolicies
{
    public const string Administrator = "Administrator";
    public const string MedicalWorker = "MedicalWorker";
    public const string Doctor = "Doctor";
    public const string DoctorOrNurse = "DoctorOrNurse";
    public const string Authenticated = "Authenticated";
}
