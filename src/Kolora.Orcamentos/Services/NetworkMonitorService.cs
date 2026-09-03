using System.Net.Http;
using System.Net.NetworkInformation;
using Serilog;

namespace Kolora.Orcamentos.Services;

public class NetworkMonitorService : INetworkMonitorService, IDisposable
{
    private readonly string _supabaseUrl;
    private readonly HttpClient _http;
    private bool _isOnline;
    public bool IsOnline => _isOnline;
    public event EventHandler<bool>? ConnectivityChanged;

    public NetworkMonitorService(IConfigurationService config)
    {
        _supabaseUrl = config.SupabaseUrl;
        _http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        NetworkChange.NetworkAvailabilityChanged += OnNetworkChanged;
        NetworkChange.NetworkAddressChanged += OnNetworkChanged;
        _ = CheckConnectivityAsync();
    }

    private void OnNetworkChanged(object? s, EventArgs e) => _ = CheckConnectivityAsync();

    public async Task<bool> CheckConnectivityAsync()
    {
        bool online = NetworkInterface.GetIsNetworkAvailable();
        if (online && !string.IsNullOrWhiteSpace(_supabaseUrl))
        {
            try
            {
                var resp = await _http.GetAsync(_supabaseUrl.TrimEnd('/') + "/rest/v1/");
                online = resp.IsSuccessStatusCode || (int)resp.StatusCode < 500;
            }
            catch { online = false; }
        }
        if (online != _isOnline)
        {
            _isOnline = online;
            Log.Information("Conectividade alterada: {Online}", _isOnline);
            ConnectivityChanged?.Invoke(this, _isOnline);
        }
        return _isOnline;
    }

    public void Dispose()
    {
        NetworkChange.NetworkAvailabilityChanged -= OnNetworkChanged;
        NetworkChange.NetworkAddressChanged -= OnNetworkChanged;
        _http.Dispose();
    }
}
