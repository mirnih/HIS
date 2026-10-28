namespace HIS.Contracts.MedicalWorkers;

public class MedicalWorkerDto
{
    public int Id { get; set; }

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string Position { get; set; } = string.Empty;

    public bool IsDeleted { get; set; }

}