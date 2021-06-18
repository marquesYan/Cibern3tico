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

        public readonly ITextIO Stdin;
        public readonly ITextIO Stdout;
        public readonly string Url;
        public readonly UdpSocket Socket;
        public readonly IPAddress PeerAddress;
        public readonly int PeerPort;

        public readonly int MaxAttempts;

        public bool HasAttempts {
            get {
                return Attempts < MaxAttempts;
            }
        }

        public SshAuthInfo(
            int maxAttempts,
            ITextIO stdin,
            ITextIO stdout,
            string url,
            UdpSocket socket,
            IPAddress peerAddress,
            int peerPort
        ) {
            MaxAttempts = maxAttempts;
            Stdin = stdin;
            Stdout = stdout;
            Url = url;
            Socket = socket;
            PeerAddress = peerAddress;
            PeerPort = peerPort;
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
                command = arguments[0];
            }

            UdpSocket socket = userSpace.Api.UdpSocket(
                outAddress,
                9999
            );

            IPAddress peerAddress = IPAddress.Parse(host);

            ITextIO stdout = userSpace.Stdout;

            SshAuthInfo authInfo = new SshAuthInfo(
                attemptCount,
                userSpace.Stdin,
                stdout,
                arguments[0],
                socket,
                peerAddress,
                peerPort
            );

            socket.ListenInput((UdpPacket packet) => {
                Debug.Log("ssh: rcv init packet");
                if (packet.Message == "ack") {
                    // Send Username
                    socket.SendTo(peerAddress, peerPort, username);

                    Authenticate(authInfo);
                }

                return false;
            }, peerAddress, peerPort);

            // Initiate connection
            socket.SendTo(peerAddress, peerPort, "init");

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
                socket.SendTo(peerAddress, peerPort, command + "\nexit\n");
                return 0;
            }

            // Open a new one to be used by ssh
            int ptFd = userSpace.Api.OpenPty();

            using (ITextIO stream = userSpace.Api.LookupByFD(ptFd)) {
                // CookPty(stream);

                socket.ListenInput(
                    (UdpPacket input) => {
                        if (eventSet) {
                            stdout.Write(input.Message);
                        }

                        return eventSet;
                    },
                    peerAddress,
                    peerPort
                );

                string key = "";

                while (eventSet && key != "exit") {
                    key = stream.ReadLine();
                    
                    socket.SendTo(peerAddress, peerPort, key + "\n");
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

            authInfo.Stdout.Write($"{authInfo.Url}: ");

            string password = authInfo.Stdin.ReadLine();

            Debug.Log("ssh: recv password: " + password);

            authInfo.Socket.ListenInput((UdpPacket packet) => {
                if (packet.Message == "1") {
                    authInfo.LoggedOut = false;
                } else {
                    Authenticate(authInfo);
                }

                return false;
            }, authInfo.PeerAddress, authInfo.PeerPort);

            authInfo.Socket.SendTo(
                authInfo.PeerAddress, 
                authInfo.PeerPort, 
                password
            );
       }

       protected void CookPty(ITextIO stream) {
            var pts = (IoctlDevice)stream;

            // Disable auto control of DownArrow character
            var downArrowArray = new string[] {
                CharacterControl.C_DDOWN_ARROW
            };
            pts.Ioctl(
                PtyIoctl.TIO_DEL_SPECIAL_CHARS,
                ref downArrowArray
            );

            // Disable auto control of UpArrow character
            var upArrowArray = new string[] {
                CharacterControl.C_DUP_ARROW
            };
            pts.Ioctl(
                PtyIoctl.TIO_DEL_SPECIAL_CHARS,
                ref upArrowArray
            );

            // Enable unbuffered operations on UpArrow character
            pts.Ioctl(
                PtyIoctl.TIO_ADD_UNBUFFERED_CHARS,
                ref upArrowArray
            );

            pts.Ioctl(
                PtyIoctl.TIO_ADD_UNBUFFERED_CHARS,
                ref downArrowArray
            );

            // Disable buffering, so we can receive the UpArrow
            // just when pressed by user
            var flagArray = new int[] { PtyFlags.BUFFERED };
            pts.Ioctl(
                PtyIoctl.TIO_UNSET_FLAG,
                ref flagArray
            );
       }
    }
}