using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace ForestProof.Testing.Api;

public sealed class BackendApiTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private static readonly string DatasetRoot = TestDataset.Locate();
    private static readonly string BackendProjectRoot = Directory.GetParent(DatasetRoot)!.FullName;

    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public BackendApiTests(WebApplicationFactory<Program> factory)
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
    public async Task PostChanges_WhenAoiId_ReturnsFeatureCollection()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/analyses/changes", new
        {
            aoiId = "RU_VOLOGDA_02",
            startYear = 2019,
            endYear = 2024
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("FeatureCollection");
    }

    [Fact]
    public async Task PostReports_WhenAoiIdWithHtmlFormat_ReturnsHtml()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/analyses/reports?format=html", new
        {
            aoiId = "RU_VOLOGDA_02",
            startYear = 2019,
            endYear = 2024,
            format = "html"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("text/html");
    }

    [Fact]
    public async Task PostAnalyses_WhenUnknownMethodProfile_ReturnsBadRequest()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/analyses", new
        {
            aoiId = "RU_VOLOGDA_02",
            startYear = 2019,
            endYear = 2024,
            methodProfile = "bogus"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PostAnalyses_WhenK2MethodProfile_ReturnsOk()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/analyses", new
        {
            aoiId = "RU_VOLOGDA_02",
            startYear = 2019,
            endYear = 2024,
            methodProfile = "k2"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetRun_WhenUnknownRunId_ReturnsNotFoundOrUnavailable()
    {
        var response = await _client.GetAsync(
            "/api/v1/runs/00000000000000000000000000000000");

        response.StatusCode.Should().BeOneOf(HttpStatusCode.NotFound, HttpStatusCode.ServiceUnavailable);
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }
}
