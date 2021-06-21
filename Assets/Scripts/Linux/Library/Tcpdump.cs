using System;
using System.Collections.Generic;
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
    public class Tcpdump : CompiledBin {
        public Tcpdump(
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
                "Usage: {0} IF",
                "Listen and dump every packet received on IF interface"
            );

            List<string> arguments = parser.Parse();

            if (arguments.Count < 1) {
                parser.ShowHelpInfo();
                return 1;
            }

            NetInterface netInterface = userSpace.Api.LookupInterface(arguments[0]);

            Predicate<Packet> listener = (Packet packet) => {
                if (packet.ProtocolID == ProtocolIdentifier.LINK) {
                    string content = "";

                    var linkLayer = (LinkLayerPacket)packet;

                    content += $"link src: {linkLayer.SrcMacAddress} ";
                    content += $"dst: {linkLayer.DstMacAddress}\n";

                    switch (packet.NextLayer.ProtocolID) {
                        case ProtocolIdentifier.ARP: {
                            var arpLayer = (ArpPacket)packet.NextLayer;

                            content += $"\tarp peer: {arpLayer.PeerAddress} ";
                            content += $"mac: {arpLayer.PeerMacAddress}\n";
                            break;
                        }

                        case ProtocolIdentifier.IP: {
                            var ipLayer = (IpPacket)packet.NextLayer;

                            content += $"\tip src: {ipLayer.SrcAddress} ";
                            content += $"dst: {ipLayer.DstAddress}\n";

                            switch (ipLayer.NextLayer.ProtocolID) {
                                case ProtocolIdentifier.UDP: {
                                    var udpLayer = (UdpPacket)ipLayer.NextLayer;

                                    content += $"\tudp local: {udpLayer.LocalPort} ";
                                    content += $"peer: {udpLayer.PeerPort}\n";
                                    break;
                                }
                            }

                            break;
                        }
                    }

                    userSpace.Print(content);
                }

                return eventSet;
            };

            netInterface.Transport.ListenInput(listener);
            netInterface.Transport.ListenOutput(listener);

            while (eventSet) {
                Thread.Sleep(200);
            }

            return 0;
       }
    }
}