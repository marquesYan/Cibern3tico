using System.Collections.Generic;
using Linux.Sys.RunTime;
using Linux.FileSystem;
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

            List<string> arguments = parser.Parse();

            if (arguments.Count < 2) {
                parser.ShowHelpInfo();
                return 1;
            }

            int srcPort;

            if (!int.TryParse(srcPortStr, out srcPort)) {
                userSpace.Stderr.WriteLine("Source port must be a number: " + srcPortStr);
                return 2;
            }

            string ipAddress = arguments[0];
            string portStr = arguments[1];

            int port;

            if (!int.TryParse(portStr, out port)) {
                userSpace.Stderr.WriteLine("Port must be a number: " + portStr);
                return 3;
            }

            UdpSocket socket = userSpace.Api.UdpSocket(srcAddress, srcPort);

            string message = "";

            while (message != "exit") {
                message = userSpace.Stdin.ReadLine();
                socket.SendTo(ipAddress, port, message);
            }

            return 0;
        }
    }
}