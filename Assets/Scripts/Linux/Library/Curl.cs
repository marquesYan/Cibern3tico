using System;
using System.Collections.Generic;
using System.Net;
using Linux.Configuration;
using Linux.Sys.RunTime;
using Linux.FileSystem;
using Linux.IO;
using Linux.Net;
using Linux.Library.ArgumentParser;
using UnityEngine;

namespace Linux.Library
{    
    public class Curl : CompiledBin {
        public Curl(
            string absolutePath,
            int uid,
            int gid,
            int permission,
            FileType type
        ) : base(absolutePath, uid, gid, permission, type) { }

        public override int Execute(UserSpace userSpace) {
            bool eventSet = true;

            var parser = new GenericArgParser(
                userSpace,
                "Usage: {0} URL",
                "Interact with a http server"
            );

            List<string> arguments = parser.Parse();

            if (arguments.Count < 1) {
                parser.ShowHelpInfo();
                return 1;
            }

            IPAddress outAddress = userSpace.Api.GetIPAddresses()[0];

            var url = new Uri(arguments[0]);

            UdpSocket socket = userSpace.Api.UdpSocket(
                outAddress,
                6666
            );

            IPAddress peerAddress = IPAddress.Parse(url.Host);

            socket.SendTo(peerAddress, url.Port, url.PathAndQuery.TrimStart('/'));

            UdpPacket packet = socket.RecvFrom(peerAddress, url.Port);

            userSpace.Print(packet.Message);

            return 0;
       }
    }
}