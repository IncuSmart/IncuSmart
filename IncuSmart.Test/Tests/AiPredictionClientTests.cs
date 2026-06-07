using System.Net;
using System.Text;
using FluentAssertions;
using IncuSmart.API.Requests;
using IncuSmart.API.Services;

namespace IncuSmart.Test.Tests
{
    public class AiPredictionClientTests
    {
        [Fact]
        public async Task PredictSuccess_UsesSnakeCaseForFastApiAndMapsResponse()
        {
            string? requestBody = null;
            var handler = new StubHttpMessageHandler(async request =>
            {
                requestBody = await request.Content!.ReadAsStringAsync();
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        """
                        {
                          "egg_type": "chicken",
                          "total_eggs": 100,
                          "predicted_success_rate": 0.86,
                          "predicted_success_percent": 86.0,
                          "confidence": 0.7,
                          "prediction_mode": "db_knn",
                          "db_completed_seasons": 5,
                          "synthetic_references": 0,
                          "message": "Prediction ready."
                        }
                        """,
                        Encoding.UTF8,
                        "application/json")
                };
            });
            var client = new AiPredictionClient(new HttpClient(handler)
            {
                BaseAddress = new Uri("http://localhost:8001/")
            });

            var result = await client.PredictSuccess(new PredictHatchingSuccessRequest
            {
                EggType = "chicken",
                TotalEggs = 100
            });

            requestBody.Should().Contain("\"egg_type\":\"chicken\"");
            requestBody.Should().Contain("\"total_eggs\":100");
            result!.PredictedSuccessPercent.Should().Be(86.0);
            result.PredictionMode.Should().Be("db_knn");
        }

        private sealed class StubHttpMessageHandler(
            Func<HttpRequestMessage, Task<HttpResponseMessage>> handler) : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(
                HttpRequestMessage request,
                CancellationToken cancellationToken) => handler(request);
        }
    }
}
