using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
namespace DigimonNXT.Client;
public sealed class AssetRecord
{
    [JsonPropertyName("sha256")] public string Sha256 { get; set; } = "";
    [JsonPropertyName("size")] public long Size { get; set; }
    [JsonPropertyName("mime")] public string Mime { get; set; } = "application/octet-stream";
}
public sealed class AssetManifest
{
    [JsonPropertyName("protocol")] public int Protocol { get; set; }
    [JsonPropertyName("game")] public string Game { get; set; } = "";
    [JsonPropertyName("revision")] public string Revision { get; set; } = "";
    [JsonPropertyName("assets")] public Dictionary<string, AssetRecord> Assets { get; set; } = new();
}
public sealed record AssetResponse(byte[] Bytes, string Mime);
public sealed class AssetCache : IDisposable
{
    private const long MaxItem = 32 * 1024 * 1024, MaxCache = 512 * 1024 * 1024;
    private readonly Uri server;
    private readonly string bundledRoot = Path.Combine(AppContext.BaseDirectory, "GameAssets");
    private readonly string cacheRoot;
    private readonly HttpClient http = new(new HttpClientHandler { AllowAutoRedirect = false, UseCookies = false }) { Timeout = TimeSpan.FromSeconds(12) };
    private readonly SemaphoreSlim refreshLock = new(1, 1);
    private readonly ConcurrentDictionary<string, SemaphoreSlim> itemLocks = new();
    private readonly Dictionary<string, AssetRecord> bundled;
    private volatile AssetManifest? current;
    private string? manifestEtag;
    private DateTime lastRefresh = DateTime.MinValue;
    public int MatchingCount { get; private set; }
    public bool ManifestAvailable => current != null;
    public long LocalHits { get; private set; }
    public long UpdatedHits { get; private set; }
    public string Summary => ManifestAvailable ? $"Built-in assets ready · {MatchingCount:N0} matched · {LocalHits:N0} local reads" : "Server assets · optional client bridge unavailable";
    public AssetCache(Uri server)
    {
        this.server = server;
        cacheRoot = Path.Combine(ClientSettings.DataDirectory, "AssetCache", ClientSettings.ProfileKey(server));
        try { bundled = JsonSerializer.Deserialize<AssetManifest>(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "asset-manifest.json")))?.Assets ?? new(); }
        catch { bundled = new(); }
    }
    public async Task RefreshAsync(CancellationToken token, bool force = false)
    {
        if (!force && DateTime.UtcNow - lastRefresh < TimeSpan.FromMinutes(1)) return;
        if (!await refreshLock.WaitAsync(0, token)) return;
        try
        {
            if (!force && DateTime.UtcNow - lastRefresh < TimeSpan.FromMinutes(1)) return;
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
            deadline.CancelAfter(TimeSpan.FromSeconds(12));
            using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(server, "_client_manifest.php"));
            request.Headers.CacheControl = new() { NoCache = true, NoStore = true };
            if (current != null && manifestEtag != null) request.Headers.IfNoneMatch.ParseAdd(manifestEtag);
            using var result = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, deadline.Token);
            if (result.StatusCode == HttpStatusCode.NotModified)
            {
                if (current == null || manifestEtag == null) throw new InvalidDataException("Manifest returned 304 without a confirmed local version.");
                return;
            }
            if (!result.IsSuccessStatusCode || (result.Content.Headers.ContentLength ?? 0) > 8_000_000) throw new InvalidDataException("Client manifest unavailable.");
            var bytes = await ReadLimitedAsync(await result.Content.ReadAsStreamAsync(deadline.Token), 8_000_000, deadline.Token);
            var manifest = JsonSerializer.Deserialize<AssetManifest>(bytes);
            if (manifest is not { Protocol: 1, Game: "Digimon NXT" } || manifest.Assets.Count > 30_000) throw new InvalidDataException("Unexpected client manifest.");
            manifest.Assets = manifest.Assets.Where(pair => SafeAssetPath(pair.Key) && ValidRecord(pair.Value)).ToDictionary();
            MatchingCount = manifest.Assets.Count(pair => bundled.TryGetValue(pair.Key, out var file) && file.Sha256 == pair.Value.Sha256 && file.Size == pair.Value.Size);
            current = manifest;
            manifestEtag = result.Headers.ETag?.ToString();
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { throw; }
        catch { current = null; manifestEtag = null; MatchingCount = 0; }
        finally { lastRefresh = DateTime.UtcNow; refreshLock.Release(); }
    }
    public bool TryAsset(Uri url, out string relative)
    {
        relative = "";
        if (!ServerAddress.InGame(server, url)) return false;
        relative = Uri.UnescapeDataString(url.AbsolutePath[server.AbsolutePath.Length..]);
        return SafeAssetPath(relative) && current?.Assets.ContainsKey(relative) == true;
    }
    public async Task<AssetResponse?> GetAsync(Uri url, CancellationToken token)
    {
        if (!TryAsset(url, out var relative) || current?.Assets.TryGetValue(relative, out var record) != true || record == null) return null;
        var gate = itemLocks.GetOrAdd(relative, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(token);
        try
        {
            if (bundled.TryGetValue(relative, out var local) && local.Sha256 == record.Sha256 && local.Size == record.Size)
            {
                var path = Path.Combine(bundledRoot, relative.Replace('/', Path.DirectorySeparatorChar));
                var bytes = await ReadVerifiedAsync(path, record, token);
                if (bytes != null) { LocalHits++; return new(bytes, record.Mime); }
            }
            var cachedPath = Path.Combine(cacheRoot, record.Sha256);
            var cached = await ReadVerifiedAsync(cachedPath, record, token);
            if (cached != null) { UpdatedHits++; File.SetLastWriteTimeUtc(cachedPath, DateTime.UtcNow); return new(cached, record.Mime); }
            // Only a requested, manifest-listed public asset is downloaded. No game pages, sessions or login responses enter this cache.
            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
            deadline.CancelAfter(TimeSpan.FromSeconds(12));
            using var response = await http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, deadline.Token);
            if (!response.IsSuccessStatusCode || (response.Content.Headers.ContentLength ?? 0) > MaxItem) return null;
            var downloaded = await ReadLimitedAsync(await response.Content.ReadAsStreamAsync(deadline.Token), record.Size, deadline.Token);
            if (!Matches(downloaded, record)) return null;
            Directory.CreateDirectory(cacheRoot);
            await File.WriteAllBytesAsync(cachedPath + ".tmp", downloaded, token);
            File.Move(cachedPath + ".tmp", cachedPath, true);
            Prune();
            UpdatedHits++;
            return new(downloaded, record.Mime);
        }
        catch (OperationCanceledException) { return null; }
        catch (Exception e) { ClientLog.Write(e); return null; }
        finally { gate.Release(); }
    }
    private void Prune()
    {
        var files = new DirectoryInfo(cacheRoot).GetFiles().OrderBy(f => f.LastWriteTimeUtc).ToList();
        var total = files.Sum(f => f.Length);
        foreach (var file in files) { if (total <= MaxCache) break; try { var size = file.Length; file.Delete(); total -= size; } catch { } }
    }
    private static async Task<byte[]?> ReadVerifiedAsync(string path, AssetRecord record, CancellationToken token)
    {
        if (!File.Exists(path) || new FileInfo(path).Length != record.Size) return null;
        var bytes = await File.ReadAllBytesAsync(path, token);
        return Matches(bytes, record) ? bytes : null;
    }
    private static bool Matches(byte[] bytes, AssetRecord record) => bytes.LongLength == record.Size && Convert.ToHexString(SHA256.HashData(bytes)).Equals(record.Sha256, StringComparison.OrdinalIgnoreCase);
    private static bool ValidRecord(AssetRecord record) => record.Size is > 0 and <= MaxItem && record.Sha256.Length == 64 && record.Sha256.All(Uri.IsHexDigit) && record.Mime.Length <= 120 && !record.Mime.Any(char.IsControl);
    private static bool SafeAssetPath(string path) => (path.StartsWith("assets/", StringComparison.Ordinal) || path.StartsWith("html/static/", StringComparison.Ordinal) || path is "favicon.ico" or "favicon.png") && !path.Contains('\\') && !path.Contains(':') && !path.Split('/').Any(part => part is ".." or "." or "") && Path.GetExtension(path).ToLowerInvariant() is ".png" or ".jpg" or ".jpeg" or ".webp" or ".gif" or ".svg" or ".ico" or ".css" or ".js" or ".ogg" or ".mp3" or ".wav" or ".woff" or ".woff2" or ".ttf" or ".json";
    private static async Task<byte[]> ReadLimitedAsync(Stream stream, long max, CancellationToken token)
    {
        using (stream)
        using (var memory = new MemoryStream())
        {
            var buffer = new byte[64 * 1024];
            while (true) { var size = await stream.ReadAsync(buffer, token); if (size == 0) break; if (memory.Length + size > max) throw new InvalidDataException("Resource exceeded its declared size."); await memory.WriteAsync(buffer.AsMemory(0, size), token); }
            return memory.ToArray();
        }
    }
    public void Dispose() { http.Dispose(); }
}
