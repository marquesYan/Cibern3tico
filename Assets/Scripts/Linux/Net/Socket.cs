using System;
using System.Net;
using UnityEngine;

namespace Linux.Net
{
    public class IpPacket {
        public readonly IPAddress IpAddress;

        public readonly string Port;

        public readonly string Message;

        public IpPacket(string ipAddress, string port, string message) {
            IpAddress = IPAddress.Parse(ipAddress);
            Port = port;
            Message = message;
        }
    }

    public class BaseSocket
    {
        protected NetInterface Interface;

        protected IPAddress IpAddress;

        protected int Port;

        public BaseSocket(NetInterface interface_, string ipAddress, int port) {
            Interface = interface_;
            IpAddress = IPAddress.Parse(ipAddress);
            Port = port;
        }


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

        protected IpPacket Recv() {
            IpPacket ipPacket;
            string packet;

            do {
                packet = Interface.Read();
            } while (!ParsePacket(packet, out ipPacket));

            return ipPacket;
        }

        protected bool ParsePacket(string packet, out IpPacket ipPacket) {
            ipPacket = null;

            string[] tokens = packet.Split(new char[] { NetIO.SEPARATOR }, 2);

            if (tokens.Length < 3) {
                // Malformed packet
                return false;
            }

            string[] dstAddress = tokens[0].Split(':');
            string[] srcAddress = tokens[1].Split(':');

            if (dstAddress.Length != 2 || srcAddress.Length != 2) {
                // Malformed destination/source address
                return false;
            }

            if (dstAddress[0] != IpAddress.ToString() && dstAddress[1] != Port.ToString()) {
                // Message not ours
                return false;
            }

            ipPacket = new IpPacket(
                srcAddress[0],
                srcAddress[1],
                tokens[2]
            );

            return true;
        }
    }

    public class UdpSocket : BaseSocket {
        public UdpSocket(
            NetInterface interface_,
            string ipAddress,
            int port
        ) : base(interface_, ipAddress, port) { }

        public void SendTo(string ipAddress, int port, string message) {
            string packet = BuildPacket(ipAddress, port, message);
            Debug.Log("UDP: packet size:" + packet.Length);
            Interface.Send(packet);
        }

        public IpPacket RecvFrom(string ipAddress, int port) {
            IpPacket packet;
            string portStr = port.ToString();

            do {
                packet = Recv();
            } while (packet.IpAddress.ToString() != ipAddress && packet.Port != portStr);

            return packet;
        }
    }
}