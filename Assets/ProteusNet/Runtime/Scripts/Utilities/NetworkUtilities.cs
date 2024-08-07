using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;

namespace jKnepel.ProteusNet.Utilities
{
    internal static class NetworkUtilities
    {
        public static ushort FindNextAvailablePort()
        {
            using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket.Bind(new IPEndPoint(IPAddress.Loopback, 0));
            return (ushort)((IPEndPoint)socket.LocalEndPoint).Port;
        }

        public static IPAddress[] GetLocalIPAddresses(bool allowVirtualIPs = false)
        {   
            List<IPAddress> ipAddresses = new();
            foreach (var item in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (item.OperationalStatus != OperationalStatus.Up) continue; 
                var adapterProperties = item.GetIPProperties();
                if (!allowVirtualIPs && adapterProperties.GatewayAddresses.FirstOrDefault() == null) continue; 
                foreach (var ip in adapterProperties.UnicastAddresses)
                {
                    if (ip.Address.AddressFamily != AddressFamily.InterNetwork) continue;
                    ipAddresses.Add(ip.Address);
                    break;
                }
            }
            return ipAddresses.ToArray();
        }

        public static bool CheckIsLocalIPAddress(IPAddress address)
		{
            var inter = NetworkInterface
                .GetAllNetworkInterfaces()
                .First(x => x
                    .GetIPProperties()
                    .UnicastAddresses
                    .Any(i => i.Address.Equals(address))
                );
            return inter.OperationalStatus == OperationalStatus.Up;
		}
    }
}
