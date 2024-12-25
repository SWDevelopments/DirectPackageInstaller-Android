using Avalonia;
using Avalonia.Data;
using DynamicData;
using LibOrbisPkg.GP4;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using System.Xml.Linq;

namespace DirectPackageInstaller.Others
{
#pragma warning disable CA1416 // Validar a compatibilidade da plataforma
    internal class WindowsNetworkManagement : INetworkManagement
    {
        private string _Adapter = null;
        public string Adapter { 
            get => _Adapter; 
            set
            {
                _Adapter = value;
                GUID = null;
            }
        }

        string GUID = null;

        public async Task<string[]> GetValidAdapters()
        {
            return await Task.Run(_GetValidAdapters);
        }

        private string[] _GetValidAdapters()
        {
            List<string> Adapters = new List<string>();
            using ManagementClass objMC = new ManagementClass("Win32_NetworkAdapterConfiguration");
            using ManagementObjectCollection objMOC = objMC.GetInstances();

            foreach (ManagementObject objMO in objMOC)
            {
                if (IsPhysicalEthernetAdapter(objMO))
                {
                    if (GetAdapterInfoByConfig(objMO, out string Name, out _, out _, out _))
                    {
                        Adapters.Add(Name);
                    }
                }
            }

            return Adapters.ToArray();
        }

        private bool IsPhysicalEthernetAdapter(ManagementObject adapterConfig)
        {
            // Relaciona o adaptador de configuração com o adaptador físico
            string? guid = adapterConfig["SettingID"]?.ToString() ?? null;
            if (string.IsNullOrEmpty(guid))
                return false;

            if (GetAdapterInfoByGUID(guid, out string Name, out string Description, out string Type, out bool IsPhysical))
            {
                var NameDesc = (Name + Description).ToLowerInvariant();
                Type = Type?.ToLower() ?? "";

                if (NameDesc.Contains("loopback"))
                    return false;

                if (NameDesc.Contains("virtual"))
                    return false;

                if (NameDesc.Contains("wifi") || NameDesc.Contains("wi-fi"))
                    return false;

                if (NameDesc.Contains("wireless") || NameDesc.Contains("bluetooth"))
                    return false;

                if (NameDesc.Contains("vpn") || NameDesc.Contains("openvpn") || NameDesc.Contains("wireguard"))
                    return false;

                return IsPhysical && Type.Contains("ethernet");
            }

            return false;
        }
        private bool GetAdapterInfoByConfig(ManagementObject Config, out string Name, out string Description, out string Type, out bool IsPhysical)
        {
            Name = null;
            Description = null;
            Type = null;
            IsPhysical = false;

            // Relaciona o adaptador de configuração com o adaptador físico
            string? guid = Config["SettingID"]?.ToString() ?? null;
            if (string.IsNullOrEmpty(guid))
                return false;

            return GetAdapterInfoByGUID(guid, out Name, out Description, out Type, out IsPhysical);
        }

        private string GetAdapterGUID(string Name)
        {
            using ManagementObjectSearcher searcher = new ManagementObjectSearcher($"SELECT * FROM Win32_NetworkAdapter WHERE NetConnectionID like '{Name}'");

            foreach (ManagementObject adapter in searcher.Get())
            {
                if (adapter["NetConnectionID"]?.ToString()?.Equals(Name, StringComparison.InvariantCultureIgnoreCase) ?? false)
                    return adapter["GUID"]?.ToString();
            }

            return null;
        }

        private bool GetAdapterInfoByGUID(string GUID, out string Name, out string Description, out string Type, out bool IsPhysical)
        {
            Name = null;
            Description = null;
            Type = null;
            IsPhysical = false;

            using ManagementObjectSearcher searcher = new ManagementObjectSearcher($"SELECT * FROM Win32_NetworkAdapter WHERE GUID = '{GUID}'");

            foreach (ManagementObject adapter in searcher.Get())
            {
                Name = adapter["NetConnectionID"]?.ToString() ?? "";
                Description = adapter["Description"]?.ToString() ?? "";
                Type = adapter["AdapterType"]?.ToString() ?? "";
                IsPhysical = adapter["PhysicalAdapter"] is bool pa && pa;
                return true;
            }

            return false;
        }

        public async Task<bool> SetIP(string IPAddress, string SubNet)
        {
            return await Task.Run(() => _SetIP(IPAddress, SubNet)).ConfigureAwait(false);
        }

        bool _SetIP(string IPAddress, string SubNet)
        {
            try
            {
                using ManagementClass objMC = new ManagementClass("Win32_NetworkAdapterConfiguration");
                using ManagementObjectCollection objMOC = objMC.GetInstances();

                if (GUID == null)
                {
                    GUID = GetAdapterGUID(Adapter);
                }

                foreach (ManagementObject objMO in objMOC)
                {
                    var ID = objMO["SettingID"]?.ToString() ?? "";

                    if (ID == GUID)
                    {
                        ManagementBaseObject setIP;
                        ManagementBaseObject newIP = objMO.GetMethodParameters("EnableStatic");

                        newIP["IPAddress"] = new string[] { IPAddress };
                        newIP["SubnetMask"] = new string[] { SubNet };

                        setIP = objMO.InvokeMethod("EnableStatic", newIP, null);
                        return true;
                    }
                }
                return false;
            }
            catch
            {
                return true;
            }
        }

        public async Task<bool> SetGateway(string Gateway)
        {
            return await Task.Run(() => _SetGateway(Gateway)).ConfigureAwait(false);
        }        
        private bool _SetGateway(string Gateway)
        {
            try
            {
                using ManagementClass objMC = new ManagementClass("Win32_NetworkAdapterConfiguration");
                using ManagementObjectCollection objMOC = objMC.GetInstances();

                if (GUID == null)
                {
                    GUID = GetAdapterGUID(Adapter);
                }

                foreach (ManagementObject objMO in objMOC)
                {
                    var ID = objMO["SettingID"]?.ToString() ?? "";

                    if (ID == GUID)
                    {
                        ManagementBaseObject setGateway;
                        ManagementBaseObject newGateway = objMO.GetMethodParameters("SetGateways");

                        newGateway["DefaultIPGateway"] = new string[] { Gateway };
                        newGateway["GatewayCostMetric"] = new int[] { 1 };

                        setGateway = objMO.InvokeMethod("SetGateways", newGateway, null);
                        return true;
                    }
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        public void SetDNS(string NIC, string DNS)
        {
            using ManagementClass objMC = new ManagementClass("Win32_NetworkAdapterConfiguration");
            using ManagementObjectCollection objMOC = objMC.GetInstances();

            if (GUID == null)
            {
                GUID = GetAdapterGUID(Adapter);
            }

            foreach (ManagementObject objMO in objMOC)
            {
                var ID = objMO["SettingID"]?.ToString() ?? "";

                if (ID == GUID)
                {
                    // if you are using the System.Net.NetworkInformation.NetworkInterface
                    // you'll need to change this line to
                    // if (objMO["Caption"].ToString().Contains(NIC))
                    // and pass in the Description property instead of the name 
                    if (objMO["Caption"].Equals(NIC))
                    {
                        ManagementBaseObject newDNS = objMO.GetMethodParameters("SetDNSServerSearchOrder");
                        newDNS["DNSServerSearchOrder"] = DNS.Split(',');
                        ManagementBaseObject setDNS = objMO.InvokeMethod("SetDNSServerSearchOrder", newDNS, null);
                    }
                }
            }
        }

        public void setWINS(string NIC, string priWINS, string secWINS)
        {
            ManagementClass objMC = new ManagementClass("Win32_NetworkAdapterConfiguration");
            ManagementObjectCollection objMOC = objMC.GetInstances();

            if (GUID == null)
            {
                GUID = GetAdapterGUID(Adapter);
            }

            foreach (ManagementObject objMO in objMOC)
            {
                var ID = objMO["SettingID"]?.ToString() ?? "";

                if (ID == GUID)
                {
                    if (objMO["Caption"].Equals(NIC))
                    {
                        ManagementBaseObject setWINS;
                        ManagementBaseObject wins = objMO.GetMethodParameters("SetWINSServer");
                        wins.SetPropertyValue("WINSPrimaryServer", priWINS);
                        wins.SetPropertyValue("WINSSecondaryServer", secWINS);

                        setWINS = objMO.InvokeMethod("SetWINSServer", wins, null);
                    }
                }
            }
        }
        public async Task<bool> SetDHCPMode()
        {
            return await Task.Run(_EnableDHCP).ConfigureAwait(false);
        }

        private bool _EnableDHCP()
        {
            try
            {
                using ManagementClass objMC = new ManagementClass("Win32_NetworkAdapterConfiguration");
                using ManagementObjectCollection objMOC = objMC.GetInstances();

                if (GUID == null)
                {
                    GUID = GetAdapterGUID(Adapter);
                }

                foreach (ManagementObject objMO in objMOC)
                {
                    var ID = objMO["SettingID"]?.ToString() ?? "";

                    if (ID == GUID)
                    {
                        objMO.InvokeMethod("EnableDHCP", null);
                        return true;
                    }
                }
                return false;
            }
            catch
            {
                return false;
            }
        }
    }
#pragma warning restore CA1416 // Validar a compatibilidade da plataforma
}