namespace HIS.Api.Models;

public class MedicalWorkerAccount : Account
{
    public int MedicalWorkerId { get; set; }
    public MedicalWorker MedicalWorker { get; set; } = null!;

}