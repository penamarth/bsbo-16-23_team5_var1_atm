using System.Text.Json;

namespace ATM.Kernel.Storage;

public class LocalStorage
{
    private readonly string _basePath;
    private readonly JsonSerializerOptions _serializerOptions;

    public LocalStorage(string? basePath = null)
    {
        _basePath = basePath ?? Path.Combine(AppContext.BaseDirectory, "storage");
        Directory.CreateDirectory(_basePath);
        _serializerOptions = new JsonSerializerOptions
        {
            WriteIndented = true
        };
    }

    public string ResolvePath(string fileName) => Path.Combine(_basePath, fileName);

    public T LoadOrDefault<T>(string fileName, T defaultValue)
    {
        var path = ResolvePath(fileName);
        if (!File.Exists(path))
        {
            return defaultValue;
        }

        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<T>(json, _serializerOptions) ?? defaultValue;
    }

    public void Save<T>(string fileName, T data)
    {
        var path = ResolvePath(fileName);
        var json = JsonSerializer.Serialize(data, _serializerOptions);
        File.WriteAllText(path, json);
    }
}

