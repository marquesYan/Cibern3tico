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
                "Usage: {0} user@hostname",
                "Remote login client"
            );

            List<string> arguments = parser.Parse();

            if (arguments.Count < 1) {
                parser.ShowHelpInfo();
                return 1;
            }

            IPAddress outAddress = userSpace.Api.GetIPAddresses()[0];

            int port = 22;

            string[] url = arguments[0].Split('@');

            if (url.Length != 2) {
                userSpace.Stderr.WriteLine("ssh: Invalid url: " + arguments[0]);
                return 1;
            }

            string username = url[0];
            string host = url[1];

            UdpSocket socket = userSpace.Api.UdpSocket(
                outAddress,
                9999
            );

            IPAddress peerAddress = IPAddress.Parse(host);
            
            // Initiate connection
            socket.SendTo(peerAddress, port, "init");

            Thread.Sleep(200);

            // Send Username
            socket.SendTo(peerAddress, port, username);

            string password = null;
            bool loggedOut = true;

            UdpPacket packet;

            while (eventSet && loggedOut) {
                userSpace.Print($"{arguments[0]}: ", "");
                password = userSpace.Stdin.ReadLine();

                socket.SendTo(peerAddress, port, password);

                packet = socket.RecvFrom(peerAddress, port);

                loggedOut = packet.Message != "1";
            }

            if (password == null) {
                return 128;
            }

            if (loggedOut) {
                return 1;
            }

            // Open a new one to be used by ssh
            int pty = userSpace.Api.OpenPty();

            using (ITextIO stream = userSpace.Api.LookupByFD(pty)) {
                // CookPty(stream);

                ITextIO stdout = userSpace.Stdout;

                socket.ListenInput(
                    (UdpPacket input) => {
                        stdout.Write(input.Message);
                    },
                    peerAddress,
                    port
                );

                string key = "";

                while (eventSet && key != "exit") {
                    key = stream.ReadLine();
                    
                    socket.SendTo(peerAddress, port, key + "\n");
                }
            }

            // Ensure pty is not connected anymore
            userSpace.Api.RemovePty(pty);

            return 0;
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