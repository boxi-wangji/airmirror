using System.Text.Json;

namespace AirMirror;

internal sealed class ReceiverSettings
{
    public string DeviceName { get; set; } = string.Empty;

    public double? VideoAspectRatio { get; set; }

    public int? VideoWidth { get; set; }

    public int? VideoHeight { get; set; }

    public static ReceiverSettings Load(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                return JsonSerializer.Deserialize<ReceiverSettings>(File.ReadAllText(path))
                       ?? new ReceiverSettings();
            }
        }
        catch (JsonException)
        {
            // 配置损坏时使用安全默认值。
        }

        return new ReceiverSettings();
    }

    public void Save(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(path, json);
    }
}
