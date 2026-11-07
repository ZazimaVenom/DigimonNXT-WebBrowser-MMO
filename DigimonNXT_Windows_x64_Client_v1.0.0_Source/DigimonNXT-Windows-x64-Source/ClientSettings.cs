using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
namespace DigimonNXT.Client;
public sealed class ClientSettings
{
    public string Server { get; set; } = "http://127.0.0.1/DigimonNXT/";
    public int Zoom { get; set; } = 200;
    public bool AutoConnect { get; set; } = true;
    public static string DataDirectory => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DigimonNXT", "Client");
    public static string ProfileKey(Uri server) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(server.AbsoluteUri))).ToLowerInvariant()[..24];
    public static ClientSettings Load()
    {
        try { return JsonSerializer.Deserialize<ClientSettings>(File.ReadAllText(Path.Combine(DataDirectory, "settings.json"))) ?? new(); }
        catch { return new(); }
    }
    public void Save()
    {
        Directory.CreateDirectory(DataDirectory);
        var destination = Path.Combine(DataDirectory, "settings.json");
        var temp = destination + ".tmp";
        File.WriteAllText(temp, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
        File.Move(temp, destination, true);
    }
}
internal static class ClientLog
{
    internal static void Write(Exception error)
    {
        try
        {
            Directory.CreateDirectory(ClientSettings.DataDirectory);
            var path = Path.Combine(ClientSettings.DataDirectory, "client.log");
            if (File.Exists(path) && new FileInfo(path).Length > 1_000_000) File.Move(path, path + ".previous", true);
            File.AppendAllText(path, DateTime.UtcNow.ToString("O") + " " + error.GetType().Name + ": " + error.Message + Environment.NewLine);
        }
        catch { }
    }
}
