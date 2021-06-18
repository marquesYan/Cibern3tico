using System.Net;
using Linux.IO;
using Linux.Sys.IO;

namespace Linux.Net
{
    public class SocketIO : CharacterDevice {
        protected UdpSocket Socket;

        protected IPAddress PeerAddress;

        protected int PeerPort;

        public delegate string TransformPacket(UdpPacket packet);

        public SocketIO(
            UdpSocket socket,
            IPAddress peerAddress,
            int peerPort,
            TransformPacket transform
        ) : base(AccessMode.O_RDWR) {
            Socket = socket;
            PeerAddress = peerAddress;
            PeerPort = peerPort;

            socket.ListenInput((UdpPacket packet) => {
                if (!IsClosed) {
                    string message = transform(packet);
                    Buffer.Enqueue(message);
                }

                return !IsClosed;
            }, PeerAddress, PeerPort);
        }

        public SocketIO(
            UdpSocket socket,
            IPAddress peerAddress,
            int peerPort
        ) : this(socket, peerAddress, peerPort, (packet) => packet.Message) {}

        protected override void InternalTruncate() {
            //
        }

        protected override bool CanMovePointer(int newPosition) {
            return false;
        }

        protected override int InternalAppend(string data) {
            Socket.SendTo(PeerAddress, PeerPort, data);
            return data.Length;
        }
    }
}