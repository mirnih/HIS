using System.Net.Http.Json;
using HIS.Contracts.Diagnoses;
using HIS.Contracts.MedicalRecords;
using HIS.Contracts.MedicalWorkers;
using HIS.Contracts.Patients;

namespace HIS.Client.Services;

public class ApiClient
{
    private readonly HttpClient _http;

    public ApiClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<List<MedicalWorkerDto>> GetMedicalWorkersAsync()
    {
        return await _http.GetFromJsonAsync<List<MedicalWorkerDto>>("api/medical-workers") ?? [];
    }

    public Task<MedicalWorkerDto?> GetMedicalWorkerAsync(int id)
    {
        return _http.GetFromJsonAsync<MedicalWorkerDto>($"api/medical-workers/{id}");
    }

    public Task CreateMedicalWorkerAsync(CreateMedicalWorkerRequest request)
    {
        return SendAsync(HttpMethod.Post, "api/medical-workers", request);
    }

    public Task UpdateMedicalWorkerAsync(int id, UpdateMedicalWorkerRequest request)
    {
        return SendAsync(HttpMethod.Put, $"api/medical-workers/{id}", request);
    }

    public Task DeleteMedicalWorkerAsync(int id)
    {
        return SendAsync(HttpMethod.Delete, $"api/medical-workers/{id}");
    }

    public async Task<List<DiagnosisDto>> GetDiagnosesAsync()
    {
        return await _http.GetFromJsonAsync<List<DiagnosisDto>>("api/diagnoses") ?? [];
    }

    public Task CreateDiagnosisAsync(CreateDiagnosisRequest request)
    {
        return SendAsync(HttpMethod.Post, "api/diagnoses", request);
    }

    public Task UpdateDiagnosisAsync(int id, UpdateDiagnosisRequest request)
    {
        return SendAsync(HttpMethod.Put, $"api/diagnoses/{id}", request);
    }

    public Task DeleteDiagnosisAsync(int id)
    {
        return SendAsync(HttpMethod.Delete, $"api/diagnoses/{id}");
    }

    public async Task ImportDiagnosesAsync(Stream stream, string fileName)
    {
        using var content = new MultipartFormDataContent();
        using var fileContent = new StreamContent(stream);
        content.Add(fileContent, "file", fileName);

        using var response = await _http.PostAsync("api/diagnoses/import", content);
        await EnsureSuccessAsync(response);
    }

    public async Task<List<PatientDto>> GetPatientsAsync()
    {
        return await _http.GetFromJsonAsync<List<PatientDto>>("api/patients") ?? [];
    }

    public async Task<PatientDto> GetPatientAsync(int id)
    {
        return await _http.GetFromJsonAsync<PatientDto>($"api/patients/{id}")
            ?? throw new InvalidOperationException("Пациент не найден.");
    }

    public Task CreatePatientAsync(CreatePatientRequest request)
    {
        return SendAsync(HttpMethod.Post, "api/patients", request);
    }

    public Task UpdatePatientAsync(int id, UpdatePatientRequest request)
    {
        return SendAsync(HttpMethod.Put, $"api/patients/{id}", request);
    }

    public Task DeletePatientAsync(int id)
    {
        return SendAsync(HttpMethod.Delete, $"api/patients/{id}");
    }

    public async Task<MedicalRecordDto> CreateMedicalRecordAsync(int patientId)
    {
        using var response = await _http.PostAsync(
            $"api/patients/{patientId}/medical-record",
            null);
        await EnsureSuccessAsync(response);

        return await response.Content.ReadFromJsonAsync<MedicalRecordDto>()
            ?? throw new InvalidOperationException("Сервер не вернул созданную ЭМК.");
    }

    public async Task<MedicalRecordDto> GetMedicalRecordAsync(int patientId)
    {
        return await _http.GetFromJsonAsync<MedicalRecordDto>(
                $"api/patients/{patientId}/medical-record")
            ?? throw new InvalidOperationException("ЭМК не найдена.");
    }

    public async Task<List<MedicalRecordHistoryDto>> GetMedicalRecordHistoryAsync(int medicalRecordId)
    {
        return await _http.GetFromJsonAsync<List<MedicalRecordHistoryDto>>(
            $"api/medical-records/{medicalRecordId}/history") ?? [];
    }

    public Task AddDiagnosisAsync(int medicalRecordId, AddDiagnosisRequest request)
    {
        return SendAsync(
            HttpMethod.Post,
            $"api/medical-records/{medicalRecordId}/diagnoses",
            request);
    }

    public Task RemoveDiagnosisAsync(
        int medicalRecordId,
        int diagnosisId,
        RemoveDiagnosisRequest request)
    {
        return SendAsync(
            HttpMethod.Delete,
            $"api/medical-records/{medicalRecordId}/diagnoses/{diagnosisId}",
            request);
    }

    public Task AddPrescriptionAsync(int medicalRecordId, AddPrescriptionRequest request)
    {
        return SendAsync(
            HttpMethod.Post,
            $"api/medical-records/{medicalRecordId}/prescriptions",
            request);
    }

    public Task CancelPrescriptionAsync(
        int medicalRecordId,
        int prescriptionId,
        CancelPrescriptionRequest request)
    {
        return SendAsync(
            HttpMethod.Post,
            $"api/medical-records/{medicalRecordId}/prescriptions/{prescriptionId}/cancel",
            request);
    }

    public Task EditPrescriptionAsync(
        int medicalRecordId,
        int prescriptionId,
        EditPrescriptionRequest request)
    {
        return SendAsync(
            HttpMethod.Put,
            $"api/medical-records/{medicalRecordId}/prescriptions/{prescriptionId}",
            request);
    }

    private async Task SendAsync(HttpMethod method, string url, object? body = null)
    {
        using var request = new HttpRequestMessage(method, url);

        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        using var response = await _http.SendAsync(request);
        await EnsureSuccessAsync(response);
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var message = await response.Content.ReadAsStringAsync();

        if (string.IsNullOrWhiteSpace(message))
        {
            message = $"Ошибка сервера: {(int)response.StatusCode}.";
        }

        throw new InvalidOperationException(message);
    }
}
