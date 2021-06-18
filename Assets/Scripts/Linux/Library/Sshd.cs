using System.Collections.Generic;
using System.Net;
using System.Threading;
using Linux.Configuration;
using Linux.Sys.RunTime;
using Linux.FileSystem;
using Linux.IO;
using Linux.PseudoTerminal;
using Linux.Sys.IO;
using Linux.Net;
using Linux.Library.ArgumentParser;
using UnityEngine;

namespace Linux.Library
{
    public class SshPtySocket : SecondaryPty {
        protected UserSpace UserSpace;

        protected SocketIO Socket;

        public SshPtySocket(
            UserSpace userSpace,
            UdpSocket socket,
            IPAddress peerAddress,
            int peerPort
        ) : base(
            _ => {},
            _ => {}
        ) {
            UserSpace = userSpace;
            Socket = new SocketIO(
                socket,
                peerAddress,
                peerPort,
                ProcessPacket
            );

            Pid = new int[1];

            // Will be not used, but must be instantiated
            Flags = new int[1];
            SpecialChars = new string[32];
            UnbufferedChars = new string[32];
        }

        protected string ProcessPacket(UdpPacket packet)
        {
            string[] message = packet.Message.Split(
                new char[] { '=' },
                2,
                System.StringSplitOptions.None
            );

            if (message.Length != 2) {
                return "";
            }

            string command = message[0];
            string value = message[1];

            switch (command) {
                case "cmd": return value + "\n";

                case "signal": {
                    int signal;
                    if (int.TryParse(value, out signal)) {
                        UserSpace.Api.KillProcess(Pid[0], (ProcessSignal)signal);
                    }

                    break;
                }
            }

            return "";
        }

        protected override int InternalAppend(string data) {
            return Socket.Write(data);
        }

        protected override string InternalRead(int length) {
            return Socket.Read(length);
        }

        protected override void InternalClose() {
            Socket.Close();
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

            UdpPacket packet;

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

                            bool loggedOut = !userSpace.Api.CheckLogin(login, password);

                            loginMessage = loggedOut ? "0" : "1";

                            socket.SendTo(
                                peerAddress,
                                peerPort,
                                loginMessage
                            );

                            if (!loggedOut) {
                                // Handle "ssh" protocol
                                var sock = new SshPtySocket(
                                    userSpace,
                                    socket,
                                    peerAddress,
                                    peerPort
                                );

                                int sockFd = userSpace.Api.OpenStream(sock);

                                userSpace.Api.RunLogin(
                                    login,
                                    password,
                                    sockFd,
                                    sockFd,
                                    sockFd
                                );
                            }

                            return eventSet && loggedOut;   // Loop while logged out
                        }, peerAddress, peerPort);

                        return false;      // Listen just once
                    }, peerAddress, peerPort);

                    socket.SendTo(peerAddress, peerPort, "ack");
                }

                return eventSet;    // Loop forever
            });

            while (eventSet) {
                Thread.Sleep(1000);
            }

            return 0;
       }
    }
}