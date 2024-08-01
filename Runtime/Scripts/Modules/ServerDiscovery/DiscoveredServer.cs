using System;
using System.Net;

namespace jKnepel.ProteusNet.Modules.ServerDiscovery
{
    public class DiscoveredServer
    {
        public IPEndPoint Endpoint { get; }
        public string Servername { get; }
        public uint MaxNumberConnectedClients { get; }
        public uint NumberConnectedClients { get; }
        public bool IsServerFull => NumberConnectedClients >= MaxNumberConnectedClients;
        public DateTime LastHeartbeat { get; }

        public DiscoveredServer(IPEndPoint endpoint, string servername, uint maxNumberConnectedClients, uint numberConnectedClients)
        {
            Endpoint = endpoint;
            Servername = servername;
            MaxNumberConnectedClients = maxNumberConnectedClients;
            NumberConnectedClients = numberConnectedClients;
            LastHeartbeat = DateTime.Now;
        }
    }
}
