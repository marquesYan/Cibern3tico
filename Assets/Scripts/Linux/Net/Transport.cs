using System;
using System.Collections.Concurrent;
using Linux.IO;
using Linux.Sys.IO;

namespace Linux.Net {
    public class VirtualEthernetTransport {
        protected Action<Packet> InputListeners;
        protected Action<Packet> OutputListeners;

        // public VirtualEthernetTransport() {
        //     InputListeners = new Action<Packet>();
        //     OutputListeners = new Action<Packet>();
        // }

        public void Broadcast(Packet packet) {
            OutputListeners(packet);
        }

        public void Process(Packet packet) {
            InputListeners(packet);
        }

        public void ListenOutput(Action<Packet> listener) {
            OutputListeners += listener;
        }

        public void ListenInput(Action<Packet> listener) {
            InputListeners += listener;
        }
    }
}