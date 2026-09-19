namespace ForestProof.Testing.Infrastructure;

public static class TestDataOptions
{
    public static IOptions<DataOptions> Create(string dataRoot)
    {
        var options = A.Fake<IOptions<DataOptions>>();
        A.CallTo(() => options.Value).Returns(new DataOptions
        {
            DataRoot = dataRoot,
            AreasGeoJsonFileName = "areas.geojson",
            AreasCsvFileName = "areas.csv",
            MethodologyDirectoryName = "methodology",
            BaselineCsvFileName = "baseline.csv",
            ParametersCsvFileName = "parameters.csv",
            BiomassRasterFileNamePattern = "CCI_Biomass_{year}.tif",
            ChangeRasterFileName = "CCI_Change_2019_2020.tif",
            GfcRasterFileName = "GFC_2025_v1_13.tif",
            ModisDirectoryName = "MODIS",
            SentinelDirectoryName = "Sentinel2",
            CacheDirectoryName = "cache",
            ReportOutputDirectoryName = "reports"
        });

        return options;
    }
}
