using GitHub.JPMikkers.DHCP;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;

namespace DirectPackageInstaller.Host
{
    public class DHCPHost : IDisposable
    {
        DHCPServer _server;
        IUDPSocketFactory _udpSocketFactory = new DefaultUDPSocketFactory(null);

        public static string Gateway => Address.Substring(0, Address.LastIndexOf(".")) + ".1";

        public static string Address = "192.169.0.2";
        public static string NetMask = "255.255.255.0";
        public static string PoolStart => Address;
        public static string PoolEnd => PoolStart.Substring(0, PoolStart.LastIndexOf(".")) + ".255";

        public bool Active => _server?.Active ?? false;

        public int LeaseTime = 86400;
        public int OfferTime = 30;
        public int MinimumPacketSize = 576;
        private bool _disposed;

        private void Start()
        {
            try
            {
                _disposed = false;

                _server = new DHCPServer(null, App.WorkingDirectory, _udpSocketFactory);
                _server.EndPoint = new IPEndPoint(IPAddress.Parse(Address), 67);
                _server.SubnetMask = IPAddress.Parse(NetMask);
                _server.PoolStart = IPAddress.Parse(PoolStart);
                _server.PoolEnd = IPAddress.Parse(PoolEnd);
                _server.LeaseTime = TimeSpan.FromSeconds(LeaseTime);
                _server.OfferExpirationTime = TimeSpan.FromSeconds(Math.Max(1, OfferTime));
                _server.MinimumPacketSize = MinimumPacketSize;

                List<OptionItem> options =
                [
                    new OptionItem() {
                            Mode = OptionMode.Default,
                            Option = new DHCPOptionRouter()
                            {
                                IPAddresses = new[] { IPAddress.Parse(Gateway) },
                                ZeroTerminatedStrings = false
                            }
                    },
                ];
                
                _server.Options = options;

                _server.OnStatusChange += server_OnStatusChange;
                _server.Start();
            }
            catch (Exception ex)
            {
                CleanupAndRetry();
            }
        }
        private void CleanupAndRetry()
        {
            if (!_disposed)
            {
                Dispose();
            }

            StartInBackground();
        }

        public void StartInBackground()
        {
            if (_server?.Active ?? false)
                return;

            new Thread(Start).Start();
        }

        public void Stop()
        {
            Dispose();
        }

        public event Action<IPAddress, IPAddress?>? OnNewClient;

        private int totalClients = 0;
        private void server_OnStatusChange(object? sender, DHCPStopEventArgs? e)
        {
            if (_server.Clients.Count != totalClients) {

                if (_server.Clients.Count > totalClients)
                {
                    var newClient = _server.Clients.OrderByDescending(x => x.LeaseStartTime).First();
                    OnNewClient?.Invoke(newClient.IPAddress, _server.EndPoint.Address);
                }

                totalClients = _server.Clients.Count;
            }
        }
        public void Dispose()
        {
            if (_server != null)
            {
                try
                {
                    if (_server.Active)
                        _server.Stop();

                    _server.OnStatusChange -= server_OnStatusChange;
                    _server.Dispose();
                    _server = null;
                }
                catch { }
            }

            _disposed = true;
        }
    }
}
