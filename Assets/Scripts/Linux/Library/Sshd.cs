using System.Collections.Generic;
using System.Net;
using Linux.Configuration;
using Linux.Sys.RunTime;
using Linux.FileSystem;
using Linux.IO;
using Linux.Net;
using Linux.Library.ArgumentParser;
using UnityEngine;

namespace Linux.Library
{
    public class SocketIO : AbstractTextIO {
        protected UdpSocket Socket;

        protected IPAddress PeerAddress;

        protected IPAddress BindAddress;

        protected int BindPort;

        protected int PeerPort;

        public SocketIO(
            UdpSocket socket,
            IPAddress peerAddress,
            int peerPort,
            IPAddress bindAddress,
            int bindPort
        ) : base(AccessMode.O_RDWR) {
            Socket = socket;
            PeerAddress = peerAddress;
            PeerPort = peerPort;
            BindAddress = bindAddress;
            BindPort = bindPort;
        }

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

        protected override string InternalRead(int length) {
            Debug.Log($"socketio: reading from: {BindAddress}:{BindPort}");
            UdpPacket packet = Socket.RecvFrom(BindAddress, BindPort);
            Debug.Log($"socketio: rcv msg: " + packet.Message);
            return packet.Message;
        }

        protected override void InternalClose() {
            //
        }
    }

    public class Sshd : CompiledBin {
        public Sshd(
            string absolutePath,
            int uid,
            int gid,
            int permission,
            FileType type
        ) : base(absolutePath, uid, gid, permission, type) { }

        public override int Execute(UserSpace userSpace) {
            Linux.Kernel kernel = userSpace.Api.AccessKernel();

            bool eventSet = true;

            userSpace.Api.Trap(
                ProcessSignal.SIGTERM,
                (int[] args) => {
                    eventSet = false;
                }
            );

            var parser = new GenericArgParser(
                userSpace,
                "Usage: {0} [-p PORT]",
                "Secure shell daemon"
            );

            IPAddress outAddress = userSpace.Api.GetIPAddresses()[0];
            parser.AddArgument<string>(
                "o|out-address=",
                "The ip address to send responses. Default is the first interface",
                address => outAddress = IPAddress.Parse(address)
            );

            IPAddress listenAddress = IPAddress.Parse("0.0.0.0");
            parser.AddArgument<string>(
                "b|bind-address=",
                "The ip address to listen. Default is all interfaces",
                address => listenAddress = IPAddress.Parse(address)
            );

            string portStr = "22";
            parser.AddArgument<string>(
                "p|port=",
                "The port to listen. Default is 22",
                (string port) => port = portStr
            );

            List<string> arguments = parser.Parse();

            int port;

            if (!int.TryParse(portStr, out port)) {
                userSpace.Stderr.WriteLine("sshd: Port must be a number: " + portStr);
                return 1;
            }

            UdpSocket socket = userSpace.Api.UdpSocket(outAddress, port);

            SocketIO io;
            UdpPacket packet;

            while (eventSet) {
                packet = socket.RecvFrom(listenAddress, port);

                if (packet.Message == "init") {
                    IpPacket ipPacket = (IpPacket)packet.NextLayer;

                    io = new SocketIO(
                        socket,
                        IPAddress.Parse(ipPacket.SrcAddress),
                        packet.LocalPort,
                        listenAddress,
                        port
                    );

                    kernel.Fs.Create(
                        "/dev/socket",
                        0, 0,
                        Perm.FromInt(7, 5, 5),
                        FileType.F_SCK,
                        io
                    );

                    int fd = userSpace.Api.Open("/dev/socket", AccessMode.O_RDWR);

                    int pid = userSpace.Api.StartProcess(
                        new string[] {
                            "/usr/bin/bash"
                        },
                        fd,
                        fd,
                        fd
                    );

                    userSpace.Api.WaitPid(pid);
                }
            }

            return 0;
       }
    }
}