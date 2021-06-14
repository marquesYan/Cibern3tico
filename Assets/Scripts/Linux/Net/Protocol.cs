namespace Linux.Net {
    public class ProtocolIdentifier {
        public const int LINK = 0x1;

        public const int ARP = 0xa;

        public const int IP = 0xb;

        public const int UDP = 0xc;
    }

    public class Packet {
        public int ProtocolID;

        public Packet NextLayer;
    }

    public class LinkLayerPacket : Packet {
        public const string EMPTY_MAC_ADDRESS = "00:00:00:00:00:00";

        public int ProtocolID = ProtocolIdentifier.LINK;

        public Packet NextLayer;

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
        }

        public LinkLayerPacket(
            string srcMacAddress,
            Packet nextLayer
        ) : this(srcMacAddress, EMPTY_MAC_ADDRESS, nextLayer) { }
    }

    public class IpPacket : Packet {
        public int ProtocolID = ProtocolIdentifier.IP;

        public string SrcAddress;

        public string DstAddress;

        public Packet NextLayer;

        public IpPacket(
            string srcAddress,
            string dstAddress,
            Packet nextLayer
        ) {
            SrcAddress = srcAddress;
            DstAddress = dstAddress;
            NextLayer = nextLayer;
        }
    }

    public class ArpPacket : Packet {
        public int ProtocolID = ProtocolIdentifier.ARP;

        public string PeerAddress;

        public string PeerMacAddress;

        public ArpPacket(string peerAddress, string peerMacAddress) {
            PeerAddress = peerAddress;
            PeerMacAddress = peerMacAddress;
        }

        public ArpPacket(string peerAddress) : this(peerAddress, LinkLayerPacket.EMPTY_MAC_ADDRESS) {}
    }

    public class UdpPacket : Packet {
        public int ProtocolID = ProtocolIdentifier.UDP;

        public int PeerPort;

        public int LocalPort;

        public string Message;

        public UdpPacket(int localPort, int peerPort, string message) {
            PeerPort = peerPort;
            LocalPort = localPort;
            Message = message;
        }
    }
}