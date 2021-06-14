namespace Linux.Net {
    public class ProtocolIdentifier {
        public const int LINK = 1;

        public const int ARP = 2;

        public const int IP = 3;

        public const int UDP = 4;
    }

    public class Packet {
        public int ProtocolID;

        public Packet NextLayer;
    }

    public class LinkLayerPacket : Packet {
        public const string EMPTY_MAC_ADDRESS = "00:00:00:00:00:00";

        public string SrcMacAddress;

        public string DstMacAddress;

        public LinkLayerPacket(
            string srcMacAddress,
            string dstMacAddress,
            Packet nextLayer
        ) {
            SrcMacAddress = srcMacAddress;
            DstMacAddress = dstMacAddress;
            NextLayer = nextLayer;

            ProtocolID = ProtocolIdentifier.LINK;
        }

        public LinkLayerPacket(
            string srcMacAddress,
            Packet nextLayer
        ) : this(srcMacAddress, EMPTY_MAC_ADDRESS, nextLayer) { }
    }

    public class IpPacket : Packet {
        public string SrcAddress;

        public string DstAddress;

        public IpPacket(
            string srcAddress,
            string dstAddress,
            Packet nextLayer
        ) {
            SrcAddress = srcAddress;
            DstAddress = dstAddress;
            NextLayer = nextLayer;

            ProtocolID = ProtocolIdentifier.IP;
        }
    }

    public class ArpPacket : Packet {
        public string PeerAddress;

        public string PeerMacAddress;

        public ArpPacket(string peerAddress, string peerMacAddress) {
            PeerAddress = peerAddress;
            PeerMacAddress = peerMacAddress;

            ProtocolID = ProtocolIdentifier.ARP;
        }

        public ArpPacket(string peerAddress) : this(peerAddress, LinkLayerPacket.EMPTY_MAC_ADDRESS) {}
    }

    public class UdpPacket : Packet {
        public int PeerPort;

        public int LocalPort;

        public string Message;

        public UdpPacket(int localPort, int peerPort, string message) {
            PeerPort = peerPort;
            LocalPort = localPort;
            Message = message;

            ProtocolID = ProtocolIdentifier.UDP;
        }
    }
}