using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace ForestProof.Testing.Api;

public sealed class AnalysisApiTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private static readonly string DatasetRoot = TestDataset.Locate();
    private static readonly string BackendProjectRoot = Directory.GetParent(DatasetRoot)!.FullName;

    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public AnalysisApiTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseContentRoot(BackendProjectRoot);
            builder.ConfigureAppConfiguration((_, configuration) =>
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["DataOptions:DataRoot"] = DatasetRoot
                }));
        });

        _client = _factory.CreateClient();
    }

    [Fact]
    public async Task CreateAnalysis_WhenVologdaKnownAoi_ReturnsSummary()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/analyses", new
        {
            aoiId = "RU_VOLOGDA_02",
            startYear = 2019,
            endYear = 2024
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("polygonAreaHectares");
    }

    [Fact]
    public async Task CreateAnalysis_WhenUnknownAoi_ReturnsBadRequest()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/analyses", new
        {
            aoiId = "UNKNOWN",
            startYear = 2019,
            endYear = 2024
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetSummary_WhenVologdaKnownAoi_ReturnsSummary()
    {
        var response = await _client.GetAsync(
            "/api/v1/analyses/RU_VOLOGDA_02/summary?startYear=2019&endYear=2024");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var summary = await response.Content.ReadFromJsonAsync<JsonElement>();
        summary.TryGetProperty("polygonAreaHectares", out _).Should().BeTrue();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }
}
