using System.Net.Http.Json;
using System.Text.Json;

namespace IncuSmart.API.Services
{
    public interface IAiAdminClient
    {
        Task<AiTrainingSyncResult?> SyncTrainingAsync(CancellationToken ct = default);
        Task<AiRagUploadResult?> UploadRagDocumentAsync(IFormFile file, CancellationToken ct = default);
        Task<AiRagStatusResult?> GetRagStatusAsync(CancellationToken ct = default);
    }

    public record AiTrainingSyncResult(
        int ClearedCacheGroups,
        bool ClearedSyntheticCache,
        bool ClearedPrebuiltModelCache,
        string Message);

    public record AiRagUploadResult(
        string Filename,
        int ChunksAdded,
        int TotalChunksInCollection,
        string Message);

    public record AiRagStatusResult(
        string CollectionName,
        int TotalChunks,
        string Provider);

    public class AiAdminClient(HttpClient httpClient) : IAiAdminClient
    {
        private static readonly JsonSerializerOptions AiJsonOptions = new(JsonSerializerDefaults.Web)
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        };

        public async Task<AiTrainingSyncResult?> SyncTrainingAsync(CancellationToken ct = default)
        {
            using var response = await httpClient.PostAsync("admin/training/sync", null, ct);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<AiTrainingSyncResult>(AiJsonOptions, ct);
        }

        public async Task<AiRagUploadResult?> UploadRagDocumentAsync(IFormFile file, CancellationToken ct = default)
        {
            using var form = new MultipartFormDataContent();
            using var stream = file.OpenReadStream();
            using var fileContent = new StreamContent(stream);
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);
            form.Add(fileContent, "file", file.FileName);

            using var response = await httpClient.PostAsync("admin/rag/upload", form, ct);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<AiRagUploadResult>(AiJsonOptions, ct);
        }

        public async Task<AiRagStatusResult?> GetRagStatusAsync(CancellationToken ct = default)
        {
            using var response = await httpClient.GetAsync("admin/rag/status", ct);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<AiRagStatusResult>(AiJsonOptions, ct);
        }
    }
}
