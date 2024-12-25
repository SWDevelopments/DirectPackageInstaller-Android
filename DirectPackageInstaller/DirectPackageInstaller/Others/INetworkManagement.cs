using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DirectPackageInstaller.Others
{
    public interface INetworkManagement
    {
        Task<string[]> GetValidAdapters();
        string Adapter { get; set; }

        Task<bool> SetIP(string IPAddress, string SubNet);
        Task<bool> SetGateway(string Gateway);
        Task<bool> SetDHCPMode();
    }
}
