using System.Collections.Generic;
using Linux.Sys;
using UnityEngine;

namespace Linux.Net {
    public class ConnectedDevice {
        public readonly Pci Pci;

        public readonly VirtualEthernetTransport Transport;

        public ConnectedDevice(Pci pci, VirtualEthernetTransport transport) {
            Pci = pci;
            Transport = transport;
        }
    }

    public class Hub
    {
        protected List<ConnectedDevice> Devices;

        public Hub() {
            Devices = new List<ConnectedDevice>();
        }

        protected void Broadcast(Pci pci, Packet packet) {
            Debug.Log("received packet: " + ((LinkLayerPacket)packet).DstMacAddress);

            foreach (ConnectedDevice device in Devices) {
                if (device.Pci != pci) {
                    Debug.Log("sending packet through: " + packet);
                    Debug.Log("sending packet through: " + pci);
                    device.Transport.Process(packet);
                }
            }
        }

        public VirtualEthernetTransport Connect(Pci pci) {
            var transport = new VirtualEthernetTransport();

            transport.ListenOutput(
                (Packet packet) => {
                    Broadcast(pci, packet);
                }
            );

            Devices.Add(
                new ConnectedDevice(pci, transport)
            );

            return transport;
        }
    }
}