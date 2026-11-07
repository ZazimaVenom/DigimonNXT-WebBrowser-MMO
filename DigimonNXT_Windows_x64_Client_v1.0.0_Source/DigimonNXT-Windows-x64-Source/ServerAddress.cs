using System.Net;
namespace DigimonNXT.Client;
public static class ServerAddress
{
    public static Uri Parse(string input)
    {
        var value = input.Trim();
        if (string.IsNullOrEmpty(value)) throw new ArgumentException("Enter the IP address or domain of your game server.");
        if (Uri.UnescapeDataString(value).Any(char.IsControl) || value.Contains('\\')) throw new ArgumentException("Use a normal web address, without backslashes or control characters.");
        if (System.Text.RegularExpressions.Regex.IsMatch(value, "%2f|%5c|%2e", System.Text.RegularExpressions.RegexOptions.IgnoreCase) || value.Split('/').Any(part => part is ".." or ".")) throw new ArgumentException("Use a direct game folder without dot segments or encoded separators.");
        bool explicitScheme = value.Contains("://", StringComparison.Ordinal);
        if (!explicitScheme)
        {
            var host = value.Split('/')[0];
            if (IPAddress.TryParse(host, out var bare) && bare.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6) value = "[" + host + "]" + value[host.Length..];
            var probe = new Uri("http://" + value, UriKind.Absolute);
            var local = probe.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase) || IPAddress.TryParse(probe.Host.Trim('[', ']'), out _) || probe.Host.EndsWith(".local", StringComparison.OrdinalIgnoreCase);
            value = (local ? "http://" : "https://") + value;
        }
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https") || string.IsNullOrWhiteSpace(uri.Host))
            throw new ArgumentException("Use an HTTP or HTTPS game server, for example http://192.168.1.20/DigimonNXT/.");
        if (uri.Port is < 1 or > 65535) throw new ArgumentException("Use a server port from 1 to 65535.");
        if (uri.UserInfo.Length > 0 || uri.Query.Length > 0 || uri.Fragment.Length > 0)
            throw new ArgumentException("Enter only the server address and game folder, without a username, password, query or # fragment.");
        var authorityStart = value.IndexOf("://", StringComparison.Ordinal) + 3;
        bool hasExplicitPath = value.IndexOf('/', authorityStart) >= 0;
        var path = hasExplicitPath ? uri.AbsolutePath : "/DigimonNXT/";
        if (path.EndsWith(".php", StringComparison.OrdinalIgnoreCase)) path = path[..(path.LastIndexOf('/') + 1)];
        if (!path.EndsWith('/')) path += "/";
        return new UriBuilder(uri) { Path = path }.Uri;
    }
    public static bool SameOrigin(Uri left, Uri right) => left.Scheme == right.Scheme && left.Host.Equals(right.Host, StringComparison.OrdinalIgnoreCase) && left.Port == right.Port;
    public static bool InGame(Uri server, Uri target) => SameOrigin(server, target) && target.AbsolutePath.StartsWith(server.AbsolutePath, StringComparison.Ordinal);
}
