namespace ForestProof.Testing.Data;

public sealed class BaselineReaderTests : IDisposable
{
    private readonly TempDataset _dataset = new();
    private readonly IOptions<DataOptions> _options;

    public BaselineReaderTests()
    {
        _dataset.WriteFile(
            "methodology/baseline.csv",
            "baseline_id,aoi_id,year_start,year_end,pool,reference_mean_2015_tc_ha,reference_mean_2019_tc_ha,historical_rate_tc_ha_yr\n" +
            "HIST-1,RU_TEST_01,2019,2020,AGB,83.752114581,85.127113698,0.343749779\n" +
            "HIST-1,RU_TEST_01,2020,2021,AGB,83.752114581,85.127113698,0.343749779\n");

        _options = TestDataOptions.Create(_dataset.RootPath);
    }

    [Fact]
    public void ReadBaseline_WhenIdExists_ReturnsReferenceMeans()
    {
        // Arrange
        var systemUnderTests = new BaselineReader(_options);

        // Act
        var baseline = systemUnderTests.ReadBaseline("RU_TEST_01");

        // Assert
        baseline.AoiId.Should().Be("RU_TEST_01");
        baseline.ReferenceMean2015.Should().BeApproximately(83.752114581, 1e-9);
        baseline.ReferenceMean2019.Should().BeApproximately(85.127113698, 1e-9);
        baseline.HistoricalRate.Should().BeApproximately(0.343749779, 1e-9);
    }

    [Fact]
    public void ReadBaseline_WhenIdMissing_Throws()
    {
        // Arrange
        var systemUnderTests = new BaselineReader(_options);

        // Act
        Action act = () => systemUnderTests.ReadBaseline("UNKNOWN");

        // Assert
        act.Should().Throw<KeyNotFoundException>();
    }

    [Fact]
    public void ReadBaselines_WhenValueNotFinite_Throws()
    {
        // Arrange
        _dataset.WriteFile(
            "methodology/baseline.csv",
            "baseline_id,aoi_id,year_start,year_end,pool,reference_mean_2015_tc_ha,reference_mean_2019_tc_ha,historical_rate_tc_ha_yr\n" +
            "HIST-1,RU_TEST_01,2019,2020,AGB,NaN,85.127113698,0.343749779\n");
        var systemUnderTests = new BaselineReader(_options);

        // Act
        Action act = () => systemUnderTests.ReadBaselines();

        // Assert
        act.Should().Throw<FormatException>();
    }

    public void Dispose()
    {
        _dataset.Dispose();
    }
}
