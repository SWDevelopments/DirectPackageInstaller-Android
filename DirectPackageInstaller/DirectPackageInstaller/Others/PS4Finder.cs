using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace DirectPackageInstaller
{
    public static class PS4Finder
    {
        public const string SearchMessage = "SRCH * HTTP/1.1\ndevice-discovery-protocol-version:00030010\n";
        public static byte[] SearchData => Encoding.UTF8.GetBytes(SearchMessage);

        public static async Task StartFinder(Action<IPAddress, IPAddress?, int> OnFound)
        {
            if (OnFound == null)
                return;

            var IPs = IPHelper.EnumLocalIPs();

            var Found = OnFound;

            bool Searching = true;

            SocketAsyncEventArgs OnReceived = new SocketAsyncEventArgs();
            OnReceived.Completed += (sender, e) =>
            {
                if (e.RemoteEndPoint is IPEndPoint)
                {
                    var socket = sender as Socket;

                    if (socket is null)
                        return;

                    var PS4IP = ((IPEndPoint) e.RemoteEndPoint).Address;

                    IPAddress? PCIP = null;

                    string message = null;

                    int SysVer = 4;
                    
                    //In case of the socket already disposed 
                    try
                    {
                        if (App.IsAndroid)
                        {
                            //If the socket is already disposed, on android, it's better to call a function
                            //to let the exception be thrown here rather than inside LocalEndPoint Property
                            //that due the LLVM otimization may fire as fatal exception
                            socket.GetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Error);
                        }

                        PCIP = ((IPEndPoint?) socket.LocalEndPoint)?.Address;
                        
                        var receivedData = e.MemoryBuffer.Slice(0, e.BytesTransferred).ToArray();
                        message = Encoding.UTF8.GetString(receivedData);

                        if (message.Contains("host-type"))
                        {
                            message = message.Substring(message.IndexOf("host-type"));
                            message = message.Substring(0, message.IndexOf("\n"));

                            if (message.Contains("PS5"))
                                SysVer = 5;
                        }
                    } catch { }


                    if (PCIP!= null && PCIP.Equals(IPAddress.Any))
                        PCIP = null;
                    
                    if (!PS4IP.Equals(IPAddress.Any))
                    {
                        Found(PS4IP, PCIP, SysVer);
                        Searching = false;
                    }
                }
            };


            int IPIndex = 0;
            
            while (Searching)
            {
                var LocalIP = IPs.Length == 0 ? null : IPs[IPIndex++];

                if (IPIndex >= IPs.Length)
                    IPIndex = 0;
                
                Socket Discovery = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                Discovery.EnableBroadcast = true;
                Discovery.ExclusiveAddressUse = false;
                Discovery.ReceiveTimeout = 3000;

                if (LocalIP != null)
                {
                    Discovery.Bind(new IPEndPoint(IPAddress.Parse(LocalIP), 0));
                    Discovery.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, 1);
                    Discovery.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.DontRoute, 1);
                }

                var broadcastAddress = IPAddress.Broadcast;

                if (App.IsAndroid)
                    broadcastAddress = IPAddress.Parse(App.GetBroadcastAddress(LocalIP));

                byte[] Buffer = new byte[Discovery.ReceiveBufferSize];
                OnReceived.SetBuffer(Buffer);
                OnReceived.RemoteEndPoint = new IPEndPoint(IPAddress.Any, 0);

                Discovery.ReceiveFromAsync(OnReceived);


                for (int i = 0; i < 5; i++)
                {
                    //PS4
                    var SendTo = new SocketAsyncEventArgs()
                    {
                        RemoteEndPoint = new IPEndPoint(broadcastAddress, 987)
                    };

                    SendTo.SetBuffer(SearchData, 0, SearchData.Length);
                    Discovery.SendToAsync(SendTo);

                    //PS5
                    SendTo = new SocketAsyncEventArgs()
                    {
                        RemoteEndPoint = new IPEndPoint(broadcastAddress, 9302)
                    };

                    SendTo.SetBuffer(SearchData, 0, SearchData.Length);
                    Discovery.SendToAsync(SendTo);

                    await Task.Delay(500);
                }

                await Task.Delay(1000);
                Discovery.Close();
            }
        }
    }
    
}
