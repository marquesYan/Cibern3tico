using System;
using System.Net;
using System.Threading;
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

        protected Packet Recv() {
            Packet packet = null;
            bool packetRecv = false;

            Interface.Transport.ListenInput(
                (Packet input) => {
                    packet = input;

                    packetRecv = true;

                    return false;   // Will read just the first packet
                }
            );

            while (!packetRecv) {
                Thread.Sleep(200);
            }

            return packet;
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

        public UdpPacket RecvFrom(IPAddress peerAddress, int peerPort) {
            Packet packet = null;

            while (packet == null && Linux.Kernel.IsRunning) {
                packet = Recv();

                if (!(IsUdpPacket(packet) 
                    && MatchesPeerAddress(packet, peerAddress, peerPort))) {
                    Debug.Log("udp: packet not for me: " + packet);
                    // Put packet back in queue
                    Interface.Transport.Process(packet);
                    packet = null;
                }
            }

            if (packet == null) {
                return null;
            }

            IpPacket ipPacket = (IpPacket)packet.NextLayer;
            UdpPacket udpPacket = (UdpPacket)ipPacket.NextLayer;

            udpPacket.NextLayer = ipPacket;

            return udpPacket;
        }

        public void ListenInput(
            Predicate<UdpPacket> listener,
            IPAddress peerAddress,
            int peerPort
        ) {
            Predicate<Packet> wrapper = (Packet packet) => {
                if (IsUdpPacket(packet) &&
                    MatchesSocketAddress(packet) &&
                    MatchesPeerAddress(packet, peerAddress, peerPort)
                ) {
                    IpPacket ipPacket = (IpPacket)packet.NextLayer;
                    UdpPacket udpPacket = (UdpPacket)ipPacket.NextLayer;

                    udpPacket.NextLayer = ipPacket;

                    return listener(udpPacket);
                }

                return true;
            };

            Interface.Transport.ListenInput(wrapper);
        }

        public void ListenAnyInput(Predicate<UdpPacket> listener) {
            ListenInput(
                listener,
                IPAddress.Parse("0.0.0.0"),
                0
            );
        }

        protected bool MatchesSocketAddress(Packet packet) {
            IpPacket ipPacket = (IpPacket)packet.NextLayer;
            UdpPacket udpPacket = (UdpPacket)ipPacket.NextLayer;

            return (IpAddress.ToString() == "0.0.0.0" ||
                        ipPacket.DstAddress == IpAddress.ToString())
                    && udpPacket.PeerPort == Port;
        }

        protected bool MatchesPeerAddress(
            Packet packet,
            IPAddress peerAddress,
            int peerPort
        ) {
            IpPacket ipPacket = (IpPacket)packet.NextLayer;
            UdpPacket udpPacket = (UdpPacket)ipPacket.NextLayer;

            return (peerAddress.ToString() == "0.0.0.0" ||
                    ipPacket.SrcAddress == peerAddress.ToString())
                    && (peerPort == 0 || udpPacket.LocalPort == peerPort);
        }

        protected bool IsUdpPacket(Packet packet) {
            return packet.ProtocolID == ProtocolIdentifier.LINK
                && packet.NextLayer?.ProtocolID == ProtocolIdentifier.IP
                && packet.NextLayer?.NextLayer?.ProtocolID == ProtocolIdentifier.UDP;
        }
    }
}