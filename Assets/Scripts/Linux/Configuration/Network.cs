using System.Collections.Generic;
using Linux.FileSystem;
using Linux.Sys;
using Linux.Sys.Net;
using Linux.IO;
using Linux.Net;

namespace Linux.Configuration
{
    public class NetworkTable {
        protected Dictionary<string, NetInterface> Interfaces;

        protected VirtualFileTree Fs;

        protected int InterfaceCount;

        public NetworkTable(VirtualFileTree fs) {
            Fs = fs;
            InterfaceCount = 0;
            Interfaces = new Dictionary<string, NetInterface>();

            // Fs.CreateDir(
            //     "/sys/class/net",
            //     0, 0, 
            //     Perm.FromInt(7, 5, 5)
            // );
        }

        public void Add(INetDriver netDriver) {            
            Interfaces.Add($"vt{InterfaceCount}", netDriver.CreateInterface());
            InterfaceCount++;
        }

        public NetInterface LookupName(string interface_) {
            NetInterface netInterface;

            if (Interfaces.TryGetValue(interface_, out netInterface)) {
                return netInterface;
            }
            
            return null;
        }

        public NetInterface LookupIpAddress(string ipAddress) {
            foreach (KeyValuePair<string, NetInterface> kvp in Interfaces) {
                NetInterface netInterface = kvp.Value;

                if (netInterface.IpAddresses.Contains(ipAddress)) {
                    return netInterface;
                }
            }
            
            return null;
        }
    }
}