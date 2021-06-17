using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using Linux.IO;
using Linux.Sys.IO;

namespace Linux.Net {
    public class VirtualEthernetTransport {
        readonly object _transportLock = new object();

        protected List<Action<Packet>> InputListeners;
        protected List<Action<Packet>> OutputListeners;

        public VirtualEthernetTransport() {
            InputListeners = new List<Action<Packet>>();
            OutputListeners = new List<Action<Packet>>();
        }

        public void Broadcast(Packet packet) {
            MapListeners(packet, OutputListeners);
        }

        public void Process(Packet packet) {
            MapListeners(packet, InputListeners);
        }

        public void ListenOutput(Action<Packet> listener) {
            lock(_transportLock) {
                OutputListeners.Add(listener);
            }
        }

        public void ListenInput(Action<Packet> listener) {
            lock(_transportLock) {
                InputListeners.Add(listener);
            }
        }

        protected void MapListeners(Packet packet, List<Action<Packet>> listeners) {
            lock(_transportLock) {
                foreach (Action<Packet> listener in listeners) {
                    listener(packet);
                }
            }
        }
    }
}