using System;
using System.Collections.Generic;
using System.Threading;
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

            userSpace.Api.Trap(
                ProcessSignal.SIGTERM,
                (int[] args) => {
                    eventSet = false;
                }
            );

            var parser = new GenericArgParser(
                userSpace,
                "Usage: {0} URL",
                "Interact with a http server"
            );

            string timeoutStr = null;
            parser.AddArgument<string>(
                "t|timeout=",
                "Maximum time to wait for response",
                (string timeout) => timeoutStr = timeout
            );

            List<string> arguments = parser.Parse();

            if (arguments.Count < 1) {
                parser.ShowHelpInfo();
                return 1;
            }

            int timeoutSecs = -1;

            if (timeoutStr != null && !int.TryParse(timeoutStr, out timeoutSecs)) {
                userSpace.Stderr.WriteLine("curl: Timeout must be a number");
                return 2;
            }

            IPAddress outAddress = userSpace.Api.GetIPAddresses()[0];

            var url = new Uri(arguments[0]);

            UdpSocket socket = userSpace.Api.UdpSocket(
                outAddress,
                6666
            );

            IPAddress peerAddress = IPAddress.Parse(url.Host);

            bool responseRcv = false;

            socket.ListenInput((UdpPacket packet) => {
                if (eventSet) {
                    userSpace.Print(packet.Message, "");
                }

                responseRcv = true;

                return false;
            }, peerAddress, url.Port);

            socket.SendTo(
                peerAddress,
                url.Port, 
                url.PathAndQuery.TrimStart('/')
            );

            DateTime start = DateTime.Now;
            bool timedOut = false;

            int elapsed;

            while (eventSet && !responseRcv && !timedOut) {
                Thread.Sleep(200);

                if (timeoutSecs > 0) {
                    elapsed = (int)DateTime.Now.Subtract(start).TotalSeconds;
                    if (timeoutSecs < elapsed) {
                        timedOut = true;
                    }
                }
            }

            eventSet = false;

            if (timedOut) {
                return 5;
            }

            return 0;
       }
    }
}