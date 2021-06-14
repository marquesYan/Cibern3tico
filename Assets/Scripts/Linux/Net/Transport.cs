using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using Linux.IO;
using Linux.Sys.IO;

namespace Linux.Net {
    public class VirtualEthernetTransport {
        protected List<Action<Packet>> InputListeners;
        protected List<Action<Packet>> OutputListeners;

        public VirtualEthernetTransport() {
            InputListeners = new List<Action<Packet>>();
            OutputListeners = new List<Action<Packet>>();
        }

        public void Broadcast(Packet packet) {
            foreach (Action<Packet> listener in OutputListeners) {
                listener(packet);
            }
        }

        public void Process(Packet packet) {
            foreach (Action<Packet> listener in InputListeners) {
                listener(packet);
            }
        }

        public void ListenOutput(Action<Packet> listener) {
            OutputListeners.Add(listener);
        }

        public void ListenInput(Action<Packet> listener) {
            InputListeners.Add(listener);
        }
    }
}