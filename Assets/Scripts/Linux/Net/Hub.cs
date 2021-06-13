using System.Collections.Generic;
using Linux.Sys;
using UnityEngine;

namespace Linux.Net {
    // public class ConnectedDevice {
    //     public readonly Pci Pci;

    //     public readonly Kernel Kernel;

    //     public ConnectedDevice(Kernel kernel, Pci pci) {
    //         Kernel = kernel;
    //         Pci = pci;
    //     }
    // }

    public class Hub
    {
        protected Dictionary<Pci, VirtualCable> Devices;

        public Hub() {
            Devices = new Dictionary<Pci, VirtualCable>();
        }

        public void Broadcast(Pci pci, string message) {
            Debug.Log("HUB: broadcasting message: " + message);
            Debug.Log("HUB: message length: " + message.Length);

            foreach (KeyValuePair<Pci, VirtualCable> kvp in Devices) {
                Pci neighbourPci = kvp.Key;
                VirtualCable neighbourVtCable = kvp.Value;


                if (pci != neighbourPci) {
                    neighbourVtCable.Write(message);
                }
            }
        }

        public VirtualCable Connect(Pci pci) {
            var vtCable = new VirtualCable(
                (string message) => {
                    Broadcast(pci, message);
                }
            );

            Devices.Add(pci, vtCable);
            return vtCable;
        }
    }
}