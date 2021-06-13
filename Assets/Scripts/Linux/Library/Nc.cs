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

            List<string> arguments = parser.Parse();

            if (arguments.Count < 2) {
                parser.ShowHelpInfo();
                return 1;
            }

            string ipAddress = arguments[0];
            string portStr = arguments[1];

            int port;

            if (!int.TryParse(portStr, out port)) {
                userSpace.Stderr.WriteLine("Port must be a number");
                return 2;
            }

            UdpSocket socket = userSpace.Api.UdpSocket("10.0.0.1", 8888);

            string message = "";

            while (message != "exit") {
                message = userSpace.Stdin.ReadLine();
                socket.SendTo(ipAddress, port, message);
            }

            return 0;
        }
    }
}