using System.Collections.Generic;
using UnityEngine;

namespace Linux.Net
{
    public class NetInterface
    {
        protected string MacAddress;

        protected VirtualCable Cable;

        public List<string> IpAddresses { get; protected set; }

        public NetInterface(VirtualCable cable, string macAddress) {
            MacAddress = macAddress;
            Cable = cable;
            IpAddresses = new List<string>();
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