using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using Linux.IO;
using Linux.Sys.IO;
using UnityEngine;

namespace Linux.Net {
    public class VirtualEthernetTransport {
        readonly object _transportLock = new object();

        protected List<Predicate<Packet>> OutputListeners;

        protected List<Predicate<Packet>> InputListeners;

        public VirtualEthernetTransport() {
            OutputListeners = new List<Predicate<Packet>>();
            InputListeners = new List<Predicate<Packet>>();
        }

        public void Broadcast(Packet packet) {
            MapListeners(packet, OutputListeners);
        }

        public void Process(Packet packet) {
            packet.Ttl++;

            if (packet.Ttl < 15) {
                MapListeners(packet, InputListeners);
            }
        }

        public void ListenOutput(Predicate<Packet> listener) {
            lock(_transportLock) {
                OutputListeners.Add(listener);
            }
        }

        public void ListenInput(Predicate<Packet> listener) {
            lock(_transportLock) {
                InputListeners.Add(listener);
            }
        }

        protected void MapListeners(Packet packet, List<Predicate<Packet>> listeners) {
            lock(_transportLock) {
                foreach (Predicate<Packet> listener in listeners.ToArray()) {
                    try {
                        if (!listener(packet)) {
                            Debug.Log("transport: removing listener: " + listener);
                            listeners.Remove(listener);
                        }
                    } catch (Exception exc) {
                        Debug.Log(exc.ToString());
                    }
                }
            }
        }
    }
}