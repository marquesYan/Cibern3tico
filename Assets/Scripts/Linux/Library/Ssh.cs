using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using Linux.Configuration;
using Linux.Sys.RunTime;
using Linux.FileSystem;
using Linux.PseudoTerminal;
using Linux.Sys.Input.Drivers.Tty;
using Linux.IO;
using Linux.Sys.IO;
using Linux.Net;
using Linux.Library.ArgumentParser;
using UnityEngine;

namespace Linux.Library
{
    public class SshAuthInfo {
        public bool LoggedOut = true;

        public bool Done = false;

        public readonly UserSpace UserSpace;
        public readonly string Url;
        public readonly SshSocket Socket;

        public readonly string UserName;

        public SshAuthInfo(
            UserSpace userSpace,
            string url,
            SshSocket socket,
            string userName
        ) {
            UserSpace = userSpace;
            Url = url;
            Socket = socket;
            UserName = userName;
        }
    }

    public class SshSocket {
        public UdpSocket RawSocket;

        protected IPAddress PeerAddress;

        protected int PeerPort;

        public SshSocket(
            UdpSocket udpSocket,
            IPAddress peerAddress,
            int peerPort
        ) {
            RawSocket = udpSocket;
            PeerAddress = peerAddress;
            PeerPort = peerPort;
        }

        public void Send(string message) {
            RawSocket.SendTo(PeerAddress, PeerPort, message);
        }

        public void SendCommand(string command) {
            Send($"cmd={command}");
        }

        public void SendStdout(string text) {
            Send($"stdout={text}");
        }

        public void SendStderr(string text) {
            Send($"stderr={text}");
        }

        public void SendSignal(ProcessSignal signal) {
            Send($"signal={(int)signal}");
        }

        public void ListenInput(Predicate<UdpPacket> listener) {
            RawSocket.ListenInput(listener, PeerAddress, PeerPort);
        }

        public void Close() {
            Send("exit=");
        }
    }

    public class Ssh : CompiledBin {
        public Ssh(
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
                "Usage: {0} user@hostname [COMMAND]",
                "Remote login client"
            );

            List<string> arguments = parser.Parse();

            if (arguments.Count < 1) {
                parser.ShowHelpInfo();
                return 1;
            }

            IPAddress outAddress = userSpace.Api.GetIPAddresses()[0];

            int peerPort = 22;

            string[] url = arguments[0].Split('@');

            if (url.Length != 2) {
                userSpace.Stderr.WriteLine("ssh: Invalid url: " + arguments[0]);
                return 1;
            }

            string username = url[0];
            string host = url[1];

            string command = null;

            if (arguments.Count > 1 && !string.IsNullOrEmpty(arguments[0])) {
                command = arguments[1];
            }

            IPAddress peerAddress = IPAddress.Parse(host);

            var socket = new SshSocket(
                userSpace.Api.UdpSocket(
                    outAddress,
                    9999
                ),
                peerAddress,
                peerPort
            );

            SshAuthInfo authInfo = new SshAuthInfo(
                userSpace,
                arguments[0],
                socket,
                username
            );

            InitProtocol(authInfo);

            while (eventSet && authInfo.LoggedOut && !authInfo.Done) {
                Thread.Sleep(200);
            }

            if (!eventSet) {
                return 255;
            }

            if (authInfo.LoggedOut) {
                userSpace.Stderr.WriteLine("ssh: authentication failed");
                return 1;
            }

            if (command != null) {
                socket.SendCommand(command);
                socket.SendCommand("exit");
                socket.Close();
                return 0;
            }

            int sigintCount = 0;

            userSpace.Api.Trap(
                ProcessSignal.SIGINT,
                (int[] args) => {
                    sigintCount++;

                    if (sigintCount > 3) {
                        eventSet = false;
                    }

                    userSpace.Stdout.Write("^C");

                    socket.SendSignal(ProcessSignal.SIGINT);
                }
            );

            // Open a new one to be used by ssh
            int ptFd = userSpace.Api.OpenPty();

            try {
                using (ITextIO stream = userSpace.Api.LookupByFD(ptFd)) {
                    CookPty(userSpace, stream);

                    socket.ListenInput(
                        (UdpPacket input) => {
                            if (eventSet) {
                                userSpace.Stdout.Write(input.Message);
                            }

                            return eventSet;
                        }
                    );

                    string key = "";

                    while (eventSet) {
                        key = stream.ReadLine();
                        
                        socket.SendCommand(key);
                    }
                }
            } finally {
                // Shutdown net listener
                eventSet = false;

                // Ensure pty is not connected anymore
                userSpace.Api.RemovePty(ptFd);

                socket.Close();
            }

            return 0;
        }

        protected void InitProtocol(SshAuthInfo authInfo) {
            authInfo.Socket.ListenInput((UdpPacket packet) => {
                if (packet.Message == "ack") {
                    Authenticate(authInfo);
                }

                return false;
            });

            // Initiate connection
            authInfo.Socket.Send("init");
        }

        protected void Authenticate(SshAuthInfo authInfo) {
            // Send Username
            authInfo.Socket.Send(authInfo.UserName);

            authInfo.Socket.ListenInput((UdpPacket packet) => {
                authInfo.Done = true;

                if (packet.Message == "1") {
                    authInfo.LoggedOut = false;
                }

                return false;
            });

            string password = authInfo.UserSpace.Input($"{authInfo.Url}: ", "");
            authInfo.Socket.Send(password);
       }

       protected void CookPty(UserSpace userSpace, ITextIO stream) {
            var pts = (IoctlDevice)stream;

            int[] pidArray = new int[] { userSpace.Api.GetPid() };

            // Set pty as controlling terminal for this process
            pts.Ioctl(
                PtyIoctl.TIO_SET_PID,
                ref pidArray
            );

            // Disable auto control of DownArrow character
            // var downArrowArray = new string[] {
            //     CharacterControl.C_DDOWN_ARROW
            // };
            // pts.Ioctl(
            //     PtyIoctl.TIO_DEL_SPECIAL_CHARS,
            //     ref downArrowArray
            // );

            // // Disable auto control of UpArrow character
            // var upArrowArray = new string[] {
            //     CharacterControl.C_DUP_ARROW
            // };
            // pts.Ioctl(
            //     PtyIoctl.TIO_DEL_SPECIAL_CHARS,
            //     ref upArrowArray
            // );

            // // Enable unbuffered operations on UpArrow character
            // pts.Ioctl(
            //     PtyIoctl.TIO_ADD_UNBUFFERED_CHARS,
            //     ref upArrowArray
            // );

            // pts.Ioctl(
            //     PtyIoctl.TIO_ADD_UNBUFFERED_CHARS,
            //     ref downArrowArray
            // );

            // // Disable buffering, so we can receive the UpArrow
            // // just when pressed by user
            // var flagArray = new int[] { PtyFlags.BUFFERED };
            // pts.Ioctl(
            //     PtyIoctl.TIO_UNSET_FLAG,
            //     ref flagArray
            // );
       }
    }
}