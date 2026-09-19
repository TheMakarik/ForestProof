using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace ForestProof.Testing.Api;

public sealed class StatusApiTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private static readonly string DatasetRoot = TestDataset.Locate();
    private static readonly string BackendProjectRoot = Directory.GetParent(DatasetRoot)!.FullName;

    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public StatusApiTests(WebApplicationFactory<Program> factory)
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
    public async Task GetStatus_WhenVologdaKnownAoi_ReturnsProgress()
    {
        var response = await _client.GetAsync(
            "/api/v1/analyses/RU_VOLOGDA_02/status?startYear=2019&endYear=2024");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("progress");
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }
}
