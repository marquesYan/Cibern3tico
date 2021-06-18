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
        public int Attempts = 0;

        public readonly UserSpace UserSpace;
        public readonly string Url;
        public readonly SshSocket Socket;

        public readonly int MaxAttempts;

        public bool HasAttempts {
            get {
                return Attempts < MaxAttempts;
            }
        }

        public SshAuthInfo(
            int maxAttempts,
            UserSpace userSpace,
            string url,
            SshSocket socket
        ) {
            MaxAttempts = maxAttempts;
            UserSpace = userSpace;
            Url = url;
            Socket = socket;
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

            string attemptCountStr = "3";
            parser.AddArgument<string>(
                "c|count=",
                $"Authentication attempt count. Default is '{attemptCountStr}'",
                (string count) => attemptCountStr = count
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

            int attemptCount;

            if (!int.TryParse(attemptCountStr, out attemptCount)) {
                userSpace.Stderr.WriteLine("ssh: Attempt count must be a number");
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
                attemptCount,
                userSpace,
                arguments[0],
                socket
            );

            socket.ListenInput((UdpPacket packet) => {
                Debug.Log("ssh: rcv init packet");
                if (packet.Message == "ack") {
                    // Send Username
                    socket.Send(username);

                    Authenticate(authInfo);
                }

                return false;
            });

            // Initiate connection
            socket.Send("init");

            while (eventSet && authInfo.LoggedOut && authInfo.HasAttempts) {
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
                socket.SendSignal(ProcessSignal.SIGTERM);
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
                    Debug.Log("ssh: stream sigint count: " + sigintCount);
                    
                    socket.SendCommand(key);
                }

                // Shutdown net listener
                eventSet = false;
            }

            // Ensure pty is not connected anymore
            userSpace.Api.RemovePty(ptFd);

            return 0;
        }

        protected void Authenticate(SshAuthInfo authInfo) {
            if (!authInfo.HasAttempts) {
                return;
            }

            authInfo.Attempts++;

            string password = authInfo.UserSpace.Input($"{authInfo.Url}: ", "");

            authInfo.Socket.ListenInput((UdpPacket packet) => {
                if (packet.Message == "1") {
                    authInfo.LoggedOut = false;
                } else {
                    Authenticate(authInfo);
                }

                return false;
            });

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