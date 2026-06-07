using System.Net.Http.Json;
using System.Text.Json;
using IncuSmart.API.Requests;
using IncuSmart.API.Responses;

namespace IncuSmart.API.Services
{
    public interface IAiPredictionClient
    {
        Task<PredictHatchingSuccessResponse?> PredictSuccess(
            PredictHatchingSuccessRequest request,
            CancellationToken cancellationToken = default);
    }

    public class AiPredictionClient(HttpClient httpClient) : IAiPredictionClient
    {
        private static readonly JsonSerializerOptions AiJsonOptions = new(JsonSerializerDefaults.Web)
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        };

        public async Task<PredictHatchingSuccessResponse?> PredictSuccess(
            PredictHatchingSuccessRequest request,
            CancellationToken cancellationToken = default)
        {
            using var response = await httpClient.PostAsJsonAsync(
                "predict-success",
                request,
                AiJsonOptions,
                cancellationToken);

            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<PredictHatchingSuccessResponse>(
                AiJsonOptions,
                cancellationToken: cancellationToken);
        }
    }
}
