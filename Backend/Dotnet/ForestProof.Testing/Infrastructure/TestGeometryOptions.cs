namespace ForestProof.Testing.Infrastructure;

public static class TestGeometryOptions
{
    public static IOptions<GeometryOptions> Create()
    {
        var options = A.Fake<IOptions<GeometryOptions>>();
        A.CallTo(() => options.Value).Returns(new GeometryOptions
        {
            SourceEpsgCode = 4326,
            TargetEpsgCode = 6933
        });

        return options;
    }
}
