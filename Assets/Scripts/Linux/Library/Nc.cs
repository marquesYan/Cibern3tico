using System.Collections.Generic;
using System.Net;
using Linux.Sys.RunTime;
using Linux.FileSystem;
using Linux.Configuration;
using Linux.IO;
using Linux.Net;
using UnityEngine;

namespace Linux.Library
{    
    public class Nc : CompiledBin {
        public Nc(
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

            var parser = new ArgumentParser.GenericArgParser(
                userSpace,
                "Usage: {0} IP PORT", 
                "Connect remote socket to standard input and output."
            );

            string srcAddress = null;
            parser.AddArgument<string>(
                "s|souce-addr=",
                "specify source address to use",
                (string addr) => srcAddress = addr
            );

            string srcPortStr = "666";
            parser.AddArgument<string>(
                "p|souce-port=",
                "specify source port to use",
                (string port) => srcPortStr = port
            );

            bool listen = false;
            parser.AddArgument<string>(
                "l|listen",
                "bind and listen for incoming connections",
                v => listen = true
            );

            List<string> arguments = parser.Parse();

            if ((listen && arguments.Count < 1) || (!listen && arguments.Count < 2)) {
                parser.ShowHelpInfo();
                return 1;
            }

            int srcPort;

            if (!int.TryParse(srcPortStr, out srcPort)) {
                userSpace.Stderr.WriteLine("Source port must be a number: " + srcPortStr);
                return 2;
            }

            string portStr;
            int port;
            UdpSocket socket;
            UdpPacket packet;

            if (listen) {
                portStr = arguments[0];

                if (!int.TryParse(portStr, out port)) {
                    userSpace.Stderr.WriteLine("Port must be a number: " + portStr);
                    return 3;
                }

                socket = userSpace.Api.UdpSocket(
                    IPAddress.Parse(srcAddress),
                    srcPort
                );

                while (eventSet) {
                    packet = socket.RecvFrom(
                        IPAddress.Parse("0.0.0.0"),
                        port
                    );

                    userSpace.Stdout.WriteLine(packet.Message);
                }

                return 0;    
            }

            string peerAddress = arguments[0];
            portStr = arguments[1];

            if (!int.TryParse(portStr, out port)) {
                Debug.Log("wrong port number!");
                userSpace.Stderr.WriteLine("Port must be a number: " + portStr);
                return 3;
            }

            socket = userSpace.Api.UdpSocket(
                IPAddress.Parse(srcAddress), 
                srcPort
            );

            string message;

            Debug.Log("sending socket now!");
            while (eventSet) {
                Debug.Log("nc: reading stdin: " + userSpace.Stdin);
                message = userSpace.Stdin.Read();
                Debug.Log("nc: read message from stdin: " + message);
                socket.SendTo(IPAddress.Parse(peerAddress), port, message);
            }

            return 0;
        }
    }
}