using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using HIS.Contracts.Accounts;
using HIS.Contracts.Audit;
using HIS.Contracts.Auth;
using HIS.Contracts.Diagnoses;
using HIS.Contracts.MedicalRecords;
using HIS.Contracts.MedicalWorkers;
using HIS.Contracts.Patients;

namespace HIS.Client.Services;

public class ApiClient
{
    private readonly HttpClient _http;
    private readonly AuthSessionService _session;
    private readonly HisAuthenticationStateProvider _authenticationState;

    public ApiClient(
        HttpClient http,
        AuthSessionService session,
        HisAuthenticationStateProvider authenticationState)
    {
        _http = http;
        _session = session;
        _authenticationState = authenticationState;
    }

    public async Task<AdminSetupStatusDto> GetAdminSetupStatusAsync()
    {
        return await _http.GetFromJsonAsync<AdminSetupStatusDto>(
            "api/auth/admin-setup/status")
            ?? new AdminSetupStatusDto();
    }

    public async Task<AdminSetupDto> StartAdminSetupAsync(
        StartAdminSetupRequest request)
    {
        using var response = await _http.PostAsJsonAsync(
            "api/auth/admin-setup/start",
            request);
        await EnsureSuccessAsync(response);

        return await response.Content.ReadFromJsonAsync<AdminSetupDto>()
            ?? throw new InvalidOperationException(
                "Сервер не вернул TOTP-секрет.");
    }

    public async Task ConfirmAdminSetupAsync(
        ConfirmAdminSetupRequest request)
    {
        using var response = await _http.PostAsJsonAsync(
            "api/auth/admin-setup/confirm",
            request);
        await EnsureSuccessAsync(response);
    }

    public async Task LoginAsync(LoginRequest request)
    {
        using var response = await _http.PostAsJsonAsync(
            "api/auth/login",
            request);
        await EnsureSuccessAsync(response);

        var tokens = await response.Content.ReadFromJsonAsync<TokenPairDto>()
            ?? throw new InvalidOperationException(
                "Сервер не вернул токены.");

        await _session.SetTokensAsync(tokens);
        _authenticationState.NotifyUserChanged();
    }

    public async Task LogoutAsync()
    {
        await _session.InitializeAsync();

        if (_session.Tokens is not null)
        {
            try
            {
                await _http.PostAsJsonAsync(
                    "api/auth/logout",
                    new RefreshTokenRequest
                    {
                        RefreshToken = _session.Tokens.RefreshToken
                    });
            }
            catch
            {
                // Локальный выход всё равно должен завершиться.
            }
        }

        await ClearSessionAsync();
    }

    public Task<List<MedicalWorkerDto>> GetMedicalWorkersAsync() =>
        GetAsync<List<MedicalWorkerDto>>("api/medical-workers", []);

    public Task<MedicalWorkerDto?> GetMedicalWorkerAsync(int id) =>
        GetAsync<MedicalWorkerDto?>(
            $"api/medical-workers/{id}",
            null);

    public Task CreateMedicalWorkerAsync(CreateMedicalWorkerRequest request) =>
        SendAsync(HttpMethod.Post, "api/medical-workers", request);

    public Task UpdateMedicalWorkerAsync(
        int id,
        UpdateMedicalWorkerRequest request) =>
        SendAsync(HttpMethod.Put, $"api/medical-workers/{id}", request);

    public Task UpdateMedicalWorkerRoleAsync(
        int id,
        UpdateMedicalWorkerRoleRequest request) =>
        SendAsync(HttpMethod.Put, $"api/medical-workers/{id}/role", request);

    public Task DeleteMedicalWorkerAsync(int id) =>
        SendAsync(HttpMethod.Delete, $"api/medical-workers/{id}");

    public Task<List<DiagnosisDto>> GetDiagnosesAsync() =>
        GetAsync<List<DiagnosisDto>>("api/diagnoses", []);

    public Task CreateDiagnosisAsync(CreateDiagnosisRequest request) =>
        SendAsync(HttpMethod.Post, "api/diagnoses", request);

    public Task UpdateDiagnosisAsync(
        int id,
        UpdateDiagnosisRequest request) =>
        SendAsync(HttpMethod.Put, $"api/diagnoses/{id}", request);

    public Task DeleteDiagnosisAsync(int id) =>
        SendAsync(HttpMethod.Delete, $"api/diagnoses/{id}");

    public async Task ImportDiagnosesAsync(Stream stream, string fileName)
    {
        await EnsureAccessTokenAsync();

        using var content = new MultipartFormDataContent();
        using var fileContent = new StreamContent(stream);
        content.Add(fileContent, "file", fileName);
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "api/diagnoses/import")
        {
            Content = content
        };
        AddBearerToken(request);

        using var response = await _http.SendAsync(request);
        await EnsureSuccessAsync(response);
    }

    public Task<List<PatientDto>> GetPatientsAsync() =>
        GetAsync<List<PatientDto>>("api/patients", []);

    public async Task<PatientDto> GetPatientAsync(int id)
    {
        return await GetAsync<PatientDto?>(
            $"api/patients/{id}",
            null)
            ?? throw new InvalidOperationException("Пациент не найден.");
    }

    public Task CreatePatientAsync(CreatePatientRequest request) =>
        SendAsync(HttpMethod.Post, "api/patients", request);

    public Task UpdatePatientAsync(int id, UpdatePatientRequest request) =>
        SendAsync(HttpMethod.Put, $"api/patients/{id}", request);

    public Task DeletePatientAsync(int id) =>
        SendAsync(HttpMethod.Delete, $"api/patients/{id}");

    public async Task<MedicalRecordDto> CreateMedicalRecordAsync(
        int patientId)
    {
        using var response = await SendAuthorizedAsync(
            HttpMethod.Post,
            $"api/patients/{patientId}/medical-record");
        await EnsureSuccessAsync(response);

        return await response.Content.ReadFromJsonAsync<MedicalRecordDto>()
            ?? throw new InvalidOperationException(
                "Сервер не вернул созданную ЭМК.");
    }

    public async Task<MedicalRecordDto> GetMedicalRecordAsync(int patientId)
    {
        return await GetAsync<MedicalRecordDto?>(
            $"api/patients/{patientId}/medical-record",
            null)
            ?? throw new InvalidOperationException("ЭМК не найдена.");
    }

    public Task<List<MedicalRecordHistoryDto>> GetMedicalRecordHistoryAsync(
        int medicalRecordId) =>
        GetAsync<List<MedicalRecordHistoryDto>>(
            $"api/medical-records/{medicalRecordId}/history",
            []);

    public Task AddDiagnosisAsync(
        int medicalRecordId,
        AddDiagnosisRequest request) =>
        SendAsync(
            HttpMethod.Post,
            $"api/medical-records/{medicalRecordId}/diagnoses",
            request);

    public Task RemoveDiagnosisAsync(
        int medicalRecordId,
        int diagnosisId) =>
        SendAsync(
            HttpMethod.Delete,
            $"api/medical-records/{medicalRecordId}/diagnoses/{diagnosisId}");

    public Task AddPrescriptionAsync(
        int medicalRecordId,
        AddPrescriptionRequest request) =>
        SendAsync(
            HttpMethod.Post,
            $"api/medical-records/{medicalRecordId}/prescriptions",
            request);

    public Task CancelPrescriptionAsync(
        int medicalRecordId,
        int prescriptionId) =>
        SendAsync(
            HttpMethod.Post,
            $"api/medical-records/{medicalRecordId}/prescriptions/{prescriptionId}/cancel");

    public Task EditPrescriptionAsync(
        int medicalRecordId,
        int prescriptionId,
        EditPrescriptionRequest request) =>
        SendAsync(
            HttpMethod.Put,
            $"api/medical-records/{medicalRecordId}/prescriptions/{prescriptionId}",
            request);

    public Task<List<AccountDto>> GetAccountsAsync() =>
        GetAsync<List<AccountDto>>("api/accounts", []);

    public Task<AccountPasswordDto> CreateAccountAsync(
        int medicalWorkerId,
        CreateMedicalWorkerAccountRequest request) =>
        SendForJsonAsync<AccountPasswordDto>(
            HttpMethod.Post,
            $"api/accounts/medical-workers/{medicalWorkerId}",
            request);

    public Task<AccountPasswordDto> RegeneratePasswordAsync(Guid accountId) =>
        SendForJsonAsync<AccountPasswordDto>(
            HttpMethod.Post,
            $"api/accounts/{accountId}/regenerate-password");

    public Task SetAccountActiveAsync(
        Guid accountId,
        bool isActive) =>
        SendAsync(
            HttpMethod.Put,
            $"api/accounts/{accountId}/active",
            new SetAccountActiveRequest { IsActive = isActive });

    public Task<List<AuditEventDto>> GetAuditAsync() =>
        GetAsync<List<AuditEventDto>>("api/audit", []);

    private async Task<T> GetAsync<T>(string url, T fallback)
    {
        using var response = await SendAuthorizedAsync(HttpMethod.Get, url);
        await EnsureSuccessAsync(response);
        return await response.Content.ReadFromJsonAsync<T>() ?? fallback;
    }

    private async Task<T> SendForJsonAsync<T>(
        HttpMethod method,
        string url,
        object? body = null)
    {
        using var response = await SendAuthorizedAsync(method, url, body);
        await EnsureSuccessAsync(response);

        return await response.Content.ReadFromJsonAsync<T>()
            ?? throw new InvalidOperationException(
                "Сервер не вернул ожидаемые данные.");
    }

    private async Task SendAsync(
        HttpMethod method,
        string url,
        object? body = null)
    {
        using var response = await SendAuthorizedAsync(method, url, body);
        await EnsureSuccessAsync(response);
    }

    private async Task<HttpResponseMessage> SendAuthorizedAsync(
        HttpMethod method,
        string url,
        object? body = null)
    {
        await EnsureAccessTokenAsync();
        var response = await SendOnceAsync(method, url, body);

        if (response.StatusCode != HttpStatusCode.Unauthorized)
        {
            return response;
        }

        response.Dispose();

        if (!await TryRefreshAsync())
        {
            return new HttpResponseMessage(HttpStatusCode.Unauthorized);
        }

        return await SendOnceAsync(method, url, body);
    }

    private async Task<HttpResponseMessage> SendOnceAsync(
        HttpMethod method,
        string url,
        object? body)
    {
        using var request = new HttpRequestMessage(method, url);

        if (body is not null)
        {
            request.Content = JsonContent.Create(body);
        }

        AddBearerToken(request);
        return await _http.SendAsync(request);
    }

    private void AddBearerToken(HttpRequestMessage request)
    {
        if (!string.IsNullOrWhiteSpace(_session.Tokens?.AccessToken))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue(
                "Bearer",
                _session.Tokens.AccessToken);
        }
    }

    private async Task EnsureAccessTokenAsync()
    {
        await _session.InitializeAsync();

        if (_session.Tokens is not null &&
            _session.Tokens.AccessTokenExpiresAt <=
            DateTime.UtcNow.AddSeconds(10))
        {
            await TryRefreshAsync();
        }
    }

    private async Task<bool> TryRefreshAsync()
    {
        if (_session.Tokens is null ||
            _session.Tokens.RefreshTokenExpiresAt <= DateTime.UtcNow)
        {
            await ClearSessionAsync();
            return false;
        }

        try
        {
            using var response = await _http.PostAsJsonAsync(
                "api/auth/refresh",
                new RefreshTokenRequest
                {
                    RefreshToken = _session.Tokens.RefreshToken
                });

            if (!response.IsSuccessStatusCode)
            {
                await ClearSessionAsync();
                return false;
            }

            var tokens = await response.Content
                .ReadFromJsonAsync<TokenPairDto>();

            if (tokens is null)
            {
                await ClearSessionAsync();
                return false;
            }

            await _session.SetTokensAsync(tokens);
            _authenticationState.NotifyUserChanged();
            return true;
        }
        catch
        {
            await ClearSessionAsync();
            return false;
        }
    }

    private async Task ClearSessionAsync()
    {
        await _session.ClearAsync();
        _authenticationState.NotifyUserChanged();
    }

    private static async Task EnsureSuccessAsync(
        HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var message = await response.Content.ReadAsStringAsync();

        if (string.IsNullOrWhiteSpace(message))
        {
            message = response.StatusCode switch
            {
                HttpStatusCode.Unauthorized =>
                    "Требуется вход в систему.",
                HttpStatusCode.Forbidden =>
                    "У аккаунта нет права на эту операцию.",
                _ => $"Ошибка сервера: {(int)response.StatusCode}."
            };
        }

        throw new InvalidOperationException(message.Trim('"'));
    }
}
