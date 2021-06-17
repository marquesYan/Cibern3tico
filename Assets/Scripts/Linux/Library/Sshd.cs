using System.Collections.Generic;
using System.Net;
using System.Threading;
using Linux.Configuration;
using Linux.Sys.RunTime;
using Linux.FileSystem;
using Linux.IO;
using Linux.Sys.IO;
using Linux.Net;
using Linux.Library.ArgumentParser;
using UnityEngine;

namespace Linux.Library
{
    public class SocketIO : CharacterDevice {
        protected UdpSocket Socket;

        protected IPAddress PeerAddress;

        protected int PeerPort;

        public SocketIO(
            UdpSocket socket,
            IPAddress peerAddress,
            int peerPort
        ) : base(AccessMode.O_RDWR) {
            Socket = socket;
            PeerAddress = peerAddress;
            PeerPort = peerPort;

            socket.ListenInput((UdpPacket packet) => {
                Buffer.Enqueue(packet.Message);

                return !IsClosed;
            }, PeerAddress, PeerPort);
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

            bool loggedOut = true;
            IPAddress peerAddress = null;
            int peerPort = -1;

            socket.ListenAnyInput(packet => {
                if (packet.Message == "init") {
                    IpPacket ipPacket = (IpPacket)packet.NextLayer;

                    peerAddress = IPAddress.Parse(ipPacket.SrcAddress);
                    peerPort = packet.LocalPort;

                    socket.ListenInput(packet => {
                        string login = packet.Message;

                        string password = null;
                        string loginMessage;

                        socket.ListenInput(packet => {                            
                            password = packet.Message;

                            loggedOut = !userSpace.Api.CheckLogin(login, password);

                            loginMessage = loggedOut ? "0" : "1";

                            socket.SendTo(
                                peerAddress,
                                peerPort,
                                loginMessage
                            );

                            return loggedOut;   // Loop while logged out
                        }, peerAddress, peerPort);

                        return false;      // Listen just once
                    }, peerAddress, peerPort);

                    socket.SendTo(peerAddress, peerPort, "ack");
                }

                return true;    // Loop forever
            });

            int socketId = 0;

            while (eventSet) {
                if (!loggedOut) {
                    io = new SocketIO(
                        socket,
                        peerAddress,
                        peerPort
                    );

                    string socketName = $"/dev/socket{socketId}";
                    socketId++;

                    kernel.Fs.Create(
                        socketName,
                        0, 0,
                        Perm.FromInt(7, 5, 5),
                        FileType.F_SCK,
                        io
                    );

                    int fd = userSpace.Api.Open(socketName, AccessMode.O_RDWR);

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

                Thread.Sleep(2000);
            }

            return 0;
       }
    }
}