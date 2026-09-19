namespace ForestProof.Testing.Infrastructure;

public sealed class TempDataset : IDisposable
{
    public TempDataset()
    {
        RootPath = Path.Join(Path.GetTempPath(), "forestproof-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(RootPath);
    }

    public string RootPath { get; }

    public void WriteFile(string relativePath, string content)
    {
        var path = Path.Join(RootPath, relativePath);
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        File.WriteAllText(path, content);
    }

    public void Dispose()
    {
        if (Directory.Exists(RootPath))
            Directory.Delete(RootPath, recursive: true);
    }
}
