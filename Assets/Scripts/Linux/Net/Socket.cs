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
                    throw new ArgumentException(
                        "host not found: " + peerAddress
                    );
                }

                Debug.Log($"mac address of ip '{peerAddress}': {macAddress}");
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