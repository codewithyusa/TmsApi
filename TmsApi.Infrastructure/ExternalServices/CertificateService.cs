using System.Net.Http.Json;
using Polly;
using Polly.Registry;
using Microsoft.Extensions.Logging;
using TmsApi.Application.Interfaces;

namespace TmsApi.Infrastructure.ExternalServices;
public class CertificateService(
    ResiliencePipelineProvider<string> pipelineProvider,
    HttpClient httpClient,
    ILogger<CertificateService> logger)
    : ICertificateService
{
    public async Task<CertificateResult> IssueCertificateAsync(
        int studentId,
        string courseCode,
        CancellationToken ct)
    {
        var pipeline =
            pipelineProvider.GetPipeline(
                "certificate-api");


        return await pipeline.ExecuteAsync(
            async token =>
            {
                logger.LogInformation(
                    "Requesting certificate for student {StudentId}, course {CourseCode}",
                    studentId,
                    courseCode);


                using var response =
                    await httpClient.PostAsJsonAsync(
                        "/fake/certificates",
                        new
                        {
                            StudentId = studentId,
                            CourseCode = courseCode
                        },
                        token);


                // 5xx -> transient failure
                // Polly retries and circuit breaker handles it
                if ((int)response.StatusCode >= 500)
                {
                    throw new HttpRequestException(
                        $"Upstream {(int)response.StatusCode}");
                }


                // 4xx -> do NOT retry
                if (!response.IsSuccessStatusCode)
                {
                    var error =
                        await response.Content
                            .ReadAsStringAsync(token);


                    throw new InvalidOperationException(
                        $"Certificate service rejected: {(int)response.StatusCode} {error}");
                }


                return await response.Content
                    .ReadFromJsonAsync<CertificateResult>(
                        cancellationToken: token)

                    ?? throw new InvalidOperationException(
                        "Empty certificate response.");
            },
            ct);
    }
}