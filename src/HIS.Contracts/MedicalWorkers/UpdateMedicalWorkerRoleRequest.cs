using System.ComponentModel.DataAnnotations;
using HIS.Contracts.Enums;

namespace HIS.Contracts.MedicalWorkers;

public class UpdateMedicalWorkerRoleRequest
{
    [EnumDataType(typeof(MedicalWorkerRole))]
    public MedicalWorkerRole Role { get; set; }
}
