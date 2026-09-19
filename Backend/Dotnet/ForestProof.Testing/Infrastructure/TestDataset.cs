namespace ForestProof.Testing.Infrastructure;

public static class TestDataset
{
    private const string RelativeDatasetPath = "ForestProof.Backend/Dataset";

    public static string Locate()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var candidate = Path.Join(directory.FullName, RelativeDatasetPath);
            if (Directory.Exists(candidate))
                return candidate;

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException(
            $"Не удалось найти набор данных '{RelativeDatasetPath}' выше '{AppContext.BaseDirectory}'.");
    }
}
