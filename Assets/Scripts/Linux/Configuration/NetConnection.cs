using System.Collections.Generic;
using Linux.FileSystem;
using Linux.Sys;
using Linux.Sys.Net;
using Linux.IO;
using Linux.Net;

namespace Linux.Configuration
{
    public class Connection {
        public readonly NetworkAddress LocalAddress;

        public readonly NetworkAddress PeerAddress;

        public readonly int LocalPort;

        public readonly int PeerPort;

        public Connection(
            NetworkAddress localAddress,
            int localPort,
            NetworkAddress peerAddress,
            int peerPort
        ) {
            LocalAddress = localAddress;
            LocalPort = localPort;
            PeerAddress = peerAddress;
            PeerPort = peerPort;
        }
    }

    public class NetConnectionTable {
        protected List<Connection> Connections;

        protected VirtualFileTree Fs;

        public NetConnectionTable(VirtualFileTree fs) {
            Fs = fs;
            Connections = new List<Connection>();
        }

        // public void Add(INetDriver netDriver) {            
        //     Interfaces.Add($"vt{InterfaceCount}", netDriver.CreateInterface());
        //     InterfaceCount++;
        // }

        // public NetInterface LookupName(string interface_) {
        //     NetInterface netInterface;

        //     if (Interfaces.TryGetValue(interface_, out netInterface)) {
        //         return netInterface;
        //     }
            
        //     return null;
        // }

        // public NetInterface LookupIpAddress(string ipAddress) {
        //     foreach (KeyValuePair<string, NetInterface> kvp in Interfaces) {
        //         NetInterface netInterface = kvp.Value;

        //         if (netInterface.IPAddresses.Contains(ipAddress)) {
        //             return netInterface;
        //         }
        //     }
            
        //     return null;
        // }
    }
}