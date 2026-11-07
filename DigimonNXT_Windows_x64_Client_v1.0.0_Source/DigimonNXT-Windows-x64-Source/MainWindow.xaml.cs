using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Runtime.InteropServices;
using System.Windows.Interop;
namespace DigimonNXT.Client;
public partial class MainWindow : Window
{
    private readonly ClientSettings settings;
    private CancellationTokenSource? connection;
    private CancellationTokenSource session = new();
    private AssetCache? assets;
    private Uri? currentServer;
    private bool connecting, fullScreen, hasPage, loaded, autoConnectSuppressed, updatingAddress;
    private WindowState previousWindowState;
    private readonly DispatcherTimer manifestTimer = new() { Interval = TimeSpan.FromMinutes(1) };
    public MainWindow()
    {
        InitializeComponent();
        settings = ClientSettings.Load();
        ServerInput.Text = settings.Server;
        ZoomSlider.Value = Math.Clamp(settings.Zoom, 100, 250);
        AutoConnectCheck.IsChecked = settings.AutoConnect;
        ApplyZoom();
        WindowState = WindowState.Maximized;
        LoadPartner(PartnerOne, "agumon.png"); LoadPartner(PartnerTwo, "gabumon.png");
        manifestTimer.Tick += async (_, _) => {
            var cache = assets;
            if (cache == null || connecting) return;
            try { await cache.RefreshAsync(session.Token); if (ReferenceEquals(cache, assets)) UpdateStatus(); } catch (OperationCanceledException) { }
        };
    }
    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr window, int attribute, ref int value, int size);
    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        try { int dark = 1; DwmSetWindowAttribute(new WindowInteropHelper(this).Handle, 20, ref dark, sizeof(int)); }
        catch (DllNotFoundException) { }
    }
    private static void LoadPartner(Image image, string filename)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "GameAssets", "html", "static", "images", "digimon", filename);
        if (!File.Exists(path)) return;
        var bitmap = new BitmapImage(); bitmap.BeginInit(); bitmap.CacheOption = BitmapCacheOption.OnLoad; bitmap.UriSource = new Uri(path); bitmap.EndInit(); bitmap.Freeze(); image.Source = bitmap;
    }
    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        loaded = true;
        if (settings.AutoConnect) { await Task.Delay(450); if (!connecting && currentServer == null && !autoConnectSuppressed && AutoConnectCheck.IsChecked == true && ServerInput.Text == settings.Server) await ConnectAsync(); }
    }
    private async void Connect_Click(object sender, RoutedEventArgs e)
    {
        if (connecting) { connection?.Cancel(); SetNotice("Connection cancelled. Choose a server and connect when ready."); return; }
        await ConnectAsync();
    }
    private async Task ConnectAsync()
    {
        Uri target;
        try { target = ServerAddress.Parse(ServerInput.Text); }
        catch (Exception e) { SetNotice(e.Message, true); return; }
        connection?.Cancel(); connection?.Dispose(); connection = new CancellationTokenSource();
        var cancellation = connection;
        var token = cancellation.Token;
        connecting = true; ConnectButton.Content = "Cancel connection";
        RuntimeButton.Visibility = Visibility.Collapsed;
        SetNotice("Contacting " + target.Authority + "… You can cancel or change servers here.");
        StatusText.Text = "Connecting · " + target.AbsoluteUri;
        try
        {
            using var healthTimeout = CancellationTokenSource.CreateLinkedTokenSource(token);
            healthTimeout.CancelAfter(TimeSpan.FromSeconds(8));
            using var http = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false }) { Timeout = TimeSpan.FromSeconds(8) };
            using var response = await http.GetAsync(new Uri(target, "_health.php"), HttpCompletionOption.ResponseHeadersRead, healthTimeout.Token);
            if ((int)response.StatusCode is >= 300 and < 400) throw new InvalidOperationException("This address redirects. Enter the server's final HTTP or HTTPS game URL, including its game folder.");
            if ((response.Content.Headers.ContentLength ?? 0) > 64_000) throw new InvalidOperationException("This address did not return Digimon NXT server information. Check the game folder in the URL.");
            var healthText = await ReadHealthAsync(response.Content, healthTimeout.Token);
            if (healthText.Length > 64_000) throw new InvalidOperationException("This address did not return Digimon NXT server information.");
            JsonDocument health;
            try { health = JsonDocument.Parse(healthText); }
            catch { throw new InvalidOperationException("Digimon NXT was not found at this address. Include the game folder, for example http://192.168.1.20/DigimonNXT/."); }
            using (health)
            {
                if (!health.RootElement.TryGetProperty("service", out var name) || name.GetString() != "Digimon NXT") throw new InvalidOperationException("This address is not reporting a Digimon NXT game server. Check the address and game folder.");
                if (!response.IsSuccessStatusCode || !health.RootElement.TryGetProperty("ok", out var ready) || ready.ValueKind != JsonValueKind.True) throw new InvalidOperationException("The game server is online, but its setup is not ready. The server owner should start MySQL and finish Create / Repair Schema in _setup.php.");
            }
            token.ThrowIfCancellationRequested();
            SetNotice("Game server found. Preparing Microsoft Edge WebView2…");
            // Each full server URL has its own browser profile, including distinct ports and game folders.
            if (currentServer != target || GameView.CoreWebView2 == null)
            {
                session.Cancel(); session.Dispose(); session = new CancellationTokenSource();
                assets?.Dispose(); assets = null;
                hasPage = false;
                var host = (Grid)GameView.Parent;
                host.Children.Remove(GameView); GameView.Dispose();
                GameView = new WebView2 { Visibility = Visibility.Collapsed, DefaultBackgroundColor = System.Drawing.Color.FromArgb(7, 19, 34) };
                host.Children.Insert(0, GameView);
                var profile = Path.Combine(ClientSettings.DataDirectory, "BrowserProfiles", ClientSettings.ProfileKey(target));
                var environment = await CoreWebView2Environment.CreateAsync(null, profile);
                token.ThrowIfCancellationRequested();
                await GameView.EnsureCoreWebView2Async(environment);
                token.ThrowIfCancellationRequested();
                currentServer = target;
                ConfigureBrowser();
            }
            assets?.Dispose(); assets = new AssetCache(target);
            SetNotice("Checking the server's asset version…");
            await assets.RefreshAsync(token, true);
            token.ThrowIfCancellationRequested();
            settings.Server = target.AbsoluteUri; settings.AutoConnect = AutoConnectCheck.IsChecked == true; settings.Zoom = (int)ZoomSlider.Value; settings.Save();
            updatingAddress = true;
            try { ServerInput.Text = target.AbsoluteUri; }
            finally { updatingAddress = false; }
            ServerBadge.Text = target.Authority;
            ApplyZoom();
            WelcomePanel.Visibility = Visibility.Collapsed; GameView.Visibility = Visibility.Visible;
            GameView.CoreWebView2.Navigate(target.AbsoluteUri);
            manifestTimer.Start(); UpdateStatus();
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { SetNotice("Connection cancelled. Your server selection is ready to edit."); }
        catch (WebView2RuntimeNotFoundException)
        {
            SetNotice("Microsoft Edge WebView2 is needed to display the game. Install it below, then restart this client. Your game server does not need any changes.", true);
            RuntimeButton.Visibility = Visibility.Visible;
        }
        catch (HttpRequestException e)
        {
            ClientLog.Write(e);
            SetNotice("Could not reach this server. Check the address, port and network. For this PC, start Apache and MySQL in XAMPP. HTTPS certificate errors must be fixed by the server owner.", true);
        }
        catch (OperationCanceledException) { SetNotice("The server did not respond in time. Check that it is running, then try again or enter a different address.", true); }
        catch (Exception e) { ClientLog.Write(e); SetNotice(e.Message, true); }
        finally
        {
            connecting = false; ConnectButton.Content = "Connect to game →";
            if (WelcomePanel.Visibility == Visibility.Visible) StatusText.Text = "Server selection · " + (hasPage ? "Your game remains open" : "Ready when you are");
        }
    }
    private static async Task<string> ReadHealthAsync(HttpContent content, CancellationToken token)
    {
        using var stream = await content.ReadAsStreamAsync(token);
        using var buffer = new MemoryStream();
        var chunk = new byte[4096];
        while (true)
        {
            var count = await stream.ReadAsync(chunk, token);
            if (count == 0) break;
            if (buffer.Length + count > 64_000) throw new InvalidOperationException("This address did not return normal Digimon NXT server information. Check the game folder.");
            await buffer.WriteAsync(chunk.AsMemory(0, count), token);
        }
        return System.Text.Encoding.UTF8.GetString(buffer.ToArray());
    }
    private void ConfigureBrowser()
    {
        var core = GameView.CoreWebView2;
        core.Settings.IsWebMessageEnabled = false;
        core.Settings.AreHostObjectsAllowed = false;
        core.Settings.AreDevToolsEnabled = false;
        core.Settings.IsStatusBarEnabled = false;
        core.Settings.IsZoomControlEnabled = false;
        core.Settings.AreBrowserAcceleratorKeysEnabled = false;
        core.Settings.IsPasswordAutosaveEnabled = false;
        core.Settings.IsGeneralAutofillEnabled = false;
        core.Settings.UserAgent += " DigimonNXTClient/1.0.0 Windows-x64";
        core.PermissionRequested += (_, e) => e.State = CoreWebView2PermissionState.Deny;
        core.DownloadStarting += (_, e) => { e.Cancel = true; StatusText.Text = "Downloads stay in your browser. Open the game in a regular browser to download files."; };
        core.NewWindowRequested += (_, e) => { e.Handled = true; HandleLink(e.Uri); };
        core.NavigationStarting += (_, e) =>
        {
            if (!Uri.TryCreate(e.Uri, UriKind.Absolute, out var target) || target.Scheme is not ("http" or "https")) { e.Cancel = true; return; }
            if (currentServer == null || !ServerAddress.InGame(currentServer, target)) { e.Cancel = true; if (e.IsUserInitiated) OpenExternal(target); return; }
            StatusText.Text = "Loading game…";
        };
        core.NavigationCompleted += (_, e) =>
        {
            if (!e.IsSuccess || e.HttpStatusCode >= 400)
            {
                ShowOptions(false);
                SetNotice("The game page could not load" + (e.HttpStatusCode >= 400 ? " (HTTP " + e.HttpStatusCode + ")" : "") + ". Check the server connection, then reconnect. Your saved progress stays on the server.", true);
                return;
            }
            hasPage = true; ReturnButton.Visibility = Visibility.Visible; BackButton.IsEnabled = core.CanGoBack; UpdateStatus();
        };
        core.HistoryChanged += (_, _) => BackButton.IsEnabled = core.CanGoBack;
        core.ProcessFailed += (_, _) => { ShowOptions(false); SetNotice("The game display stopped. Choose Connect to game to reopen it. Server progress is safe.", true); currentServer = null; };
        core.AddWebResourceRequestedFilter("*", CoreWebView2WebResourceContext.All);
        core.WebResourceRequested += ResourceRequested;
    }
    private async void ResourceRequested(object? sender, CoreWebView2WebResourceRequestedEventArgs e)
    {
        var cache = assets;
        if (cache == null || !e.Request.Method.Equals("GET", StringComparison.OrdinalIgnoreCase) || !Uri.TryCreate(e.Request.Uri, UriKind.Absolute, out var url) || !cache.TryAsset(url, out _)) return;
        using var deferral = e.GetDeferral();
        try
        {
            var response = await cache.GetAsync(url, session.Token);
            if (response == null || !ReferenceEquals(cache, assets)) return;
            var bytes = response.Bytes;
            int status = 200;
            string reason = "OK", extra = "";
            if (e.Request.Headers.Contains("Range"))
            {
                // Single byte ranges support seeking bundled music without downloading the track again.
                var range = e.Request.Headers.GetHeader("Range");
                if (!TryRange(range, bytes.Length, out int start, out int end)) return;
                extra = $"Content-Range: bytes {start}-{end}/{bytes.Length}\r\n";
                bytes = bytes[start..(end + 1)]; status = 206; reason = "Partial Content";
            }
            var mime = response.Mime.StartsWith("text/", StringComparison.OrdinalIgnoreCase) || response.Mime is "application/javascript" or "application/json" ? response.Mime + "; charset=utf-8" : response.Mime;
            var headers = $"Content-Type: {mime}\r\nContent-Length: {bytes.Length}\r\nAccept-Ranges: bytes\r\nCache-Control: no-cache\r\nX-Content-Type-Options: nosniff\r\n{extra}";
            e.Response = GameView.CoreWebView2.Environment.CreateWebResourceResponse(new MemoryStream(bytes, false), status, reason, headers);
        }
        catch (Exception error) { ClientLog.Write(error); }
    }
    public static bool TryRange(string range, int length, out int start, out int end)
    {
        start = 0; end = length - 1;
        if (!range.StartsWith("bytes=", StringComparison.OrdinalIgnoreCase) || range.Contains(',')) return false;
        var halves = range[6..].Split('-'); if (halves.Length != 2) return false;
        if (halves[0].Length == 0)
        {
            if (!int.TryParse(halves[1], out var suffix) || suffix <= 0) return false;
            start = Math.Max(0, length - suffix); return length > 0;
        }
        if (!int.TryParse(halves[0], out start) || start < 0 || start >= length) return false;
        if (halves[1].Length > 0 && (!int.TryParse(halves[1], out end) || end < start)) return false;
        end = Math.Min(end, length - 1); return true;
    }
    private void HandleLink(string address)
    {
        if (!Uri.TryCreate(address, UriKind.Absolute, out var url) || url.Scheme is not ("https" or "http")) return;
        if (currentServer != null && ServerAddress.InGame(currentServer, url)) GameView.CoreWebView2.Navigate(url.AbsoluteUri);
        else OpenExternal(url);
    }
    private void OpenExternal(Uri url)
    {
        if (MessageBox.Show(this, "Open this link in your default browser?\n\n" + url.AbsoluteUri, "Leave the game window?", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            Process.Start(new ProcessStartInfo(url.AbsoluteUri) { UseShellExecute = true });
    }
    private void ApplyZoom()
    {
        if (ZoomLabel == null || DisplayStatus == null) return;
        var zoom = (int)ZoomSlider.Value;
        ZoomLabel.Text = zoom + "%" + (zoom == 200 ? " · 4K default" : "");
        DisplayStatus.Text = zoom + "%  •  WINDOWS x64";
        if (GameView?.CoreWebView2 != null) GameView.ZoomFactor = zoom / 100.0;
    }
    private void Zoom_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        ApplyZoom();
        if (loaded) { settings.Zoom = (int)ZoomSlider.Value; settings.Save(); }
    }
    private void UpdateStatus() { if (currentServer != null) StatusText.Text = "Connected · " + currentServer.Authority + " · " + (assets?.Summary ?? "Server assets"); }
    private void SetNotice(string message, bool error = false)
    {
        ConnectionMessage.Text = message;
        ConnectionNotice.Background = (Brush)new BrushConverter().ConvertFromString(error ? "#332032" : "#102B3A")!;
        ConnectionNotice.BorderBrush = (Brush)new BrushConverter().ConvertFromString(error ? "#A95F80" : "#28546B")!;
    }
    private void ShowOptions(bool cancelConnection = true)
    {
        if (cancelConnection) connection?.Cancel();
        GameView.Visibility = Visibility.Collapsed; WelcomePanel.Visibility = Visibility.Visible;
        ReturnButton.Visibility = hasPage ? Visibility.Visible : Visibility.Collapsed;
        ServerInput.Focus(); ServerInput.SelectAll();
    }
    private void Options_Click(object sender, RoutedEventArgs e) { autoConnectSuppressed = true; ShowOptions(); SetNotice("Choose a server or adjust your display size. Your current game stays open until you connect elsewhere."); }
    private void Return_Click(object sender, RoutedEventArgs e)
    {
        settings.AutoConnect = AutoConnectCheck.IsChecked == true; settings.Save();
        if (hasPage) { WelcomePanel.Visibility = Visibility.Collapsed; GameView.Visibility = Visibility.Visible; UpdateStatus(); }
    }
    private void Local_Click(object sender, RoutedEventArgs e) => ServerInput.Text = "http://127.0.0.1/DigimonNXT/";
    private void ServerInput_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (!loaded || updatingAddress) return;
        autoConnectSuppressed = true;
        if (connecting) connection?.Cancel();
    }
    private async void ServerInput_KeyDown(object sender, KeyEventArgs e) { if (e.Key == Key.Enter && !connecting) { e.Handled = true; await ConnectAsync(); } }
    private void Back_Click(object sender, RoutedEventArgs e) { if (GameView.CoreWebView2?.CanGoBack == true) GameView.CoreWebView2.GoBack(); }
    private void Reload_Click(object sender, RoutedEventArgs e) { if (GameView.CoreWebView2 != null && hasPage) GameView.Reload(); }
    private void FullScreen_Click(object sender, RoutedEventArgs e) => ToggleFullScreen();
    private void ToggleFullScreen()
    {
        if (!fullScreen) { previousWindowState = WindowState; WindowStyle = WindowStyle.None; ResizeMode = ResizeMode.NoResize; WindowState = WindowState.Maximized; fullScreen = true; }
        else { WindowStyle = WindowStyle.SingleBorderWindow; ResizeMode = ResizeMode.CanResize; WindowState = previousWindowState; fullScreen = false; }
    }
    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.F11) { ToggleFullScreen(); e.Handled = true; }
        else if (e.Key == Key.Escape && fullScreen) { ToggleFullScreen(); e.Handled = true; }
        else if (e.Key == Key.F5 && hasPage) { GameView.Reload(); e.Handled = true; }
        else if (e.SystemKey == Key.Left && Keyboard.Modifiers == ModifierKeys.Alt && GameView.CoreWebView2?.CanGoBack == true) { GameView.CoreWebView2.GoBack(); e.Handled = true; }
    }
    private void Runtime_Click(object sender, RoutedEventArgs e)
    {
        var installer = Path.Combine(AppContext.BaseDirectory, "Runtime", "MicrosoftEdgeWebview2Setup.exe");
        if (File.Exists(installer)) Process.Start(new ProcessStartInfo(installer) { UseShellExecute = true });
        else Process.Start(new ProcessStartInfo("https://developer.microsoft.com/microsoft-edge/webview2/") { UseShellExecute = true });
    }
    private void Window_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        try { settings.AutoConnect = AutoConnectCheck.IsChecked == true; settings.Zoom = (int)ZoomSlider.Value; settings.Save(); } catch (Exception error) { ClientLog.Write(error); }
        manifestTimer.Stop(); connection?.Cancel(); session.Cancel(); assets?.Dispose(); GameView.Dispose();
    }
}
