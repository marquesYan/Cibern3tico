using System.Collections.Generic;
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
        protected string MacAddress;

        protected VirtualCable Cable;

        public List<NetworkAddress> IPAddresses { get; protected set; }

        public NetInterface(VirtualCable cable, string macAddress) {
            MacAddress = macAddress;
            Cable = cable;
            IPAddresses = new List<NetworkAddress>();
        }

        public bool HasIPAddress(string ipAddress) {
            return IPAddresses.Find(netAddr => netAddr.IPAddress.ToString() == ipAddress) != null;
        }

        public void Send(string message) {
            string packet = BuildPacket(message);
            string[] msgArray = new string[] { packet };

            Cable.Ioctl(
                NetIO.SEND_BROADCAST,
                ref msgArray
            );
        }

        public string Read() {
            string message;
            string packet;

            do {
                packet = Cable.Read();
            } while (!ParsePacket(packet, out message));

            return message;
        }

        protected string BuildPacket(string message) {
            string mask = "{0}{1}{2}";

            return string.Format(
                mask,
                MacAddress,
                NetIO.SEPARATOR,
                message
            );
        }

        protected bool ParsePacket(string packet, out string message) {
            message = null;

            string[] tokens = packet.Split(new char[] { NetIO.SEPARATOR }, 1);

            if (tokens.Length < 2) {
                // Malformed packet
                return false;
            }

            if (tokens[0] != MacAddress) {
                // Message not ours
                return false;
            }

            message = tokens[1];

            return true;
        }
    }
}