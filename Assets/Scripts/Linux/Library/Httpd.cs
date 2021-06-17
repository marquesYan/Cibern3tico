using System.Collections.Generic;
using System.Net;
using System.Threading;
using Linux.Configuration;
using Linux.Sys.RunTime;
using Linux.FileSystem;
using Linux.IO;
using Linux.Net;
using Linux.Library.ArgumentParser;
using UnityEngine;

namespace Linux.Library
{    
    public class Httpd : CompiledBin {
        public Httpd(
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
                "Usage: {0} [-p PORT] DIR",
                "Serve directory with http server"
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

            string portStr = "80";
            parser.AddArgument<string>(
                "p|port=",
                "The port to listen. Default is 80",
                (string port) => port = portStr
            );

            List<string> arguments = parser.Parse();

            if (arguments.Count < 1) {
                parser.ShowHelpInfo();
                return 1;
            }

            int port;

            if (!int.TryParse(portStr, out port)) {
                userSpace.Stderr.WriteLine("httpd: Port must be a number: " + portStr);
                return 1;
            }

            string path = arguments[0];

            // Ensure can access directory before binding socket
            userSpace.Api.ListDirectory(path);

            UdpSocket socket = userSpace.Api.UdpSocket(outAddress, port);

            string message, response;
            UdpPacket packet;
            IpPacket ipPacket;

            socket.ListenAnyInput(packet => {
                message = packet.Message;

                if (string.IsNullOrEmpty(message)) {
                    message = "index.html";
                }

                ReadOnlyFile file = FindFile(userSpace, message, path);

                if (file == null) {
                    response = "404 not found!";
                } else {
                    using (ITextIO stream = userSpace.Open(file.Path, AccessMode.O_RDONLY)) {
                        response = stream.Read();
                    }
                }

                ipPacket = (IpPacket)packet.NextLayer;

                socket.SendTo(
                    IPAddress.Parse(ipPacket.SrcAddress),
                    packet.LocalPort,
                    response
                );

                return eventSet;
            });

            while (eventSet) {
                Thread.Sleep(1000);
            }

            return 0;
       }

       protected ReadOnlyFile FindFile(UserSpace userSpace, string fileName, string path) {
           foreach (ReadOnlyFile file in userSpace.Api.ListDirectory(path)) {
                if (file.Name == fileName) {
                    return file;
                }
            }

            return null;
       }
    }
}