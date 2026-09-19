namespace ForestProof.Testing.Data;

public sealed class ParametersReaderTests : IDisposable
{
    private readonly TempDataset _dataset = new();
    private readonly IOptions<DataOptions> _options;

    public ParametersReaderTests()
    {
        _dataset.WriteFile(
            "methodology/parameters.csv",
            "parameter,value,unit,kind,source_id,locator,applicability\n" +
            "CF_AGB,0.47,т C/т сухого вещества,опубликованное значение по умолчанию,IPCC_FOREST_2006,\"Таблица 4.3, с. 4.48, общее значение All\",Общее значение\n" +
            "CO2_per_C,44/12,т CO₂/т C,отношение молярных масс,IPCC_GENERIC_2006,Отношение молярных масс,Изменение запаса\n");

        _options = TestDataOptions.Create(_dataset.RootPath);
    }

    [Fact]
    public void ReadParameters_WhenLocatorHasCommas_KeepsQuotedValue()
    {
        // Arrange
        var systemUnderTests = new ParametersReader(_options);

        // Act
        var parameters = systemUnderTests.ReadParameters();

        // Assert
        parameters.Should().ContainKey("CF_AGB");
        parameters["CF_AGB"].Value.Should().Be("0.47");
        parameters["CF_AGB"].Locator.Should().Be("Таблица 4.3, с. 4.48, общее значение All");
        parameters.Should().ContainKey("CO2_per_C");
        parameters["CO2_per_C"].Value.Should().Be("44/12");
    }

    public void Dispose()
    {
        _dataset.Dispose();
    }
}
