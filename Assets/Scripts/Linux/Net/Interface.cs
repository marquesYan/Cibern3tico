using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Net;
using UnityEngine;

namespace Linux.Net
{
    public class NetworkAddress {
        public readonly IPAddress IPAddress;

        public readonly int Mask;

        public NetworkAddress(IPAddress ipAddress, int mask) {
            IPAddress = ipAddress;
            Mask = mask;

            if (mask < 0 || mask > 31) {
                throw new System.ArgumentException(
                    "Invalid network mask: " + mask
                );
            }
        }

        public static NetworkAddress FromString(string address) {
            string[] parsedAddr = address.Split('/');
            if (parsedAddr.Length != 2) {
                throw new System.ArgumentException(
                    "Malformed address: " + address
                );
            }

            string ip = parsedAddr[0];
            string maskStr = parsedAddr[1];

            int mask;

            if (!int.TryParse(maskStr, out mask)) {
                throw new System.ArgumentException(
                    "Mask must be a number: " + mask
                );
            }

            return new NetworkAddress(IPAddress.Parse(ip), mask);
        }
    }


    public class NetInterface
    {
        public ConcurrentQueue<Packet> InputQueue { get; protected set; }

        public string MacAddress { get; protected set; }

        public List<NetworkAddress> IPAddresses { get; protected set; }
        public VirtualEthernetTransport Transport { get; protected set; }

        public NetInterface(VirtualEthernetTransport transport, string macAddress) {
            MacAddress = macAddress;
            Transport = transport;
            IPAddresses = new List<NetworkAddress>();
            InputQueue = new ConcurrentQueue<Packet>();

            Transport.ListenInput(HandleInputPacket);
        }

        public bool HasIPAddress(string ipAddress) {
            return IPAddresses.Find(netAddr => netAddr.IPAddress.ToString() == ipAddress) != null;
        }

        protected void HandleInputPacket(Packet packet) {
            if (packet.ProtocolID == ProtocolIdentifier.LINK) {
                if (packet.NextLayer.ProtocolID == ProtocolIdentifier.ARP) {
                    HandleInputArpPacket((ArpPacket) packet.NextLayer);
                    return;
                }
            } else {
                // unknow packet
                return;
            }

            InputQueue.Enqueue(packet);
        }

        protected void HandleInputArpPacket(ArpPacket packet) {
            if (HasIPAddress(packet.PeerAddress)) {
                packet.PeerMacAddress = MacAddress;
            }
        }
    }
}