using System;
using System.Net;
using Linux.Configuration;
using UnityEngine;

namespace Linux.Net
{
    public class BaseSocket
    {
        protected NetInterface Interface;

        protected IPAddress IpAddress;

        protected ArpTable ArpTable;

        protected int Port;

        public BaseSocket(
            ArpTable arpTable,
            NetInterface interface_, 
            IPAddress ipAddress, 
            int port
        ) {
            ArpTable = arpTable;
            Interface = interface_;
            IpAddress = ipAddress;
            Port = port;
        }

        protected void SendPacket(string peerAddress, Packet packet) {
            string macAddress = ArpTable.LookupIpAddress(peerAddress);

            if (macAddress == null) {
                macAddress = AddressResolution(peerAddress);

                if (macAddress == null) {
                    throw new System.ArgumentException(
                        "host not found: " + peerAddress
                    );
                }

                ArpTable.Add(peerAddress, macAddress);
            }

            var fullLayer = new LinkLayerPacket(
                Interface.MacAddress,
                macAddress,
                packet
            );

            Interface.Transport.Broadcast(fullLayer);
        }

        protected string AddressResolution(string peerAddress) {
            var packet = new LinkLayerPacket(
                Interface.MacAddress,
                new ArpPacket(peerAddress)
            );
            
            Interface.Transport.Broadcast(packet);

            string macAddress = ((ArpPacket)packet.NextLayer).PeerMacAddress;

            if (macAddress == LinkLayerPacket.EMPTY_MAC_ADDRESS) {
                return null;
            }

            return macAddress;
        }

        // protected string BuildPacket(string message) {
        //     string mask = "{0}{1}{2}";

        //     return string.Format(
        //         mask,
        //         MacAddress,
        //         NetIO.SEPARATOR,
        //         message
        //     );
        // }

        // protected bool ParsePacket(string packet, out string message) {
        //     message = null;

        //     string[] tokens = packet.Split(new char[] { NetIO.SEPARATOR }, 1);

        //     if (tokens.Length < 2) {
        //         // Malformed packet
        //         return false;
        //     }

        //     if (tokens[0] != MacAddress) {
        //         // Message not ours
        //         return false;
        //     }

        //     message = tokens[1];

        //     return true;
        // }


        protected string BuildPacket(
            string ipAddress,
            int port,
            string message
        ) {
            IPAddress.Parse(ipAddress);

            string mask = "{0}:{1}{2}{3}:{4}{5}{6}";

            return string.Format(
                mask,
                ipAddress,
                port,
                NetIO.SEPARATOR,
                IpAddress,
                Port,
                NetIO.SEPARATOR,
                message
            );
        }

        // protected IpPacket Recv() {
        //     IpPacket ipPacket;
        //     string packet;

        //     do {
        //         packet = Interface.Recv();
        //     } while (!ParsePacket(packet, out ipPacket));

        //     return ipPacket;
        // }

        // protected bool ParsePacket(string packet, out IpPacket ipPacket) {
        //     ipPacket = null;

        //     string[] tokens = packet.Split(new char[] { NetIO.SEPARATOR }, 2);

        //     if (tokens.Length < 3) {
        //         // Malformed packet
        //         return false;
        //     }

        //     string[] dstAddress = tokens[0].Split(':');
        //     string[] srcAddress = tokens[1].Split(':');

        //     if (dstAddress.Length != 2 || srcAddress.Length != 2) {
        //         // Malformed destination/source address
        //         return false;
        //     }

        //     if (dstAddress[0] != IpAddress.ToString() && dstAddress[1] != Port.ToString()) {
        //         // Message not ours
        //         return false;
        //     }

        //     ipPacket = new IpPacket(
        //         srcAddress[0],
        //         srcAddress[1],
        //         tokens[2]
        //     );

        //     return true;
        // }
    }

    public class UdpSocket : BaseSocket {
        public UdpSocket(
            ArpTable arpTable,
            NetInterface interface_,
            IPAddress ipAddress, 
            int port
        ) : base(arpTable, interface_, ipAddress, port) { }

        public void SendTo(IPAddress peerAddress, int peerPort, string message) {
            SendPacket(
                peerAddress.ToString(),
                new IpPacket(
                    IpAddress.ToString(),
                    peerAddress.ToString(),
                    new UdpPacket(
                        Port,
                        peerPort,
                        message
                    )
                )
            );
        }
    }
}