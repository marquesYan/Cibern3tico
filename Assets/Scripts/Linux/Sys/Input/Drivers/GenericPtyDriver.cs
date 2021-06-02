using System.Collections.Generic;
using System.Threading;
using Linux.Configuration;
using Linux.IO;
using Linux.PseudoTerminal;
using Linux.Sys.IO;
using UnityEngine;

namespace Linux.Sys.Input.Drivers
{
    public class PtyCtlEntry {
        public readonly PrimaryPty Pty;
        
        public readonly SecondaryPty Pts;

        public PtyCtlEntry(
            PrimaryPty pty,
            SecondaryPty pts
        ) {
            Pty = pty;
            Pts = pts;
        }
    }

    public class GenericPtyDriver : ITtyDriver
    {
        readonly object _ptyLock = new object();

        protected Linux.Kernel Kernel;

        protected List<PtyCtlEntry> Ptys;
        protected Queue<string> InputQueue;
        protected Queue<string> OutputQueue;
        protected UEvent KbdEvent;

        public GenericPtyDriver(Linux.Kernel kernel) {
            Kernel = kernel;

            InputQueue = new Queue<string>();
            OutputQueue = new Queue<string>();
            Ptys = new List<PtyCtlEntry>();

            KbdEvent = kernel.EventTable.LookupByType(DevType.KEYBOARD);

            StartInputListener();

            if (KbdEvent != null) {
                StartKbdListener();
            }

            // UEvent consoleEvent = EventTable.LookupByType(DevType.CONSOLE);
        }

        public bool IsSupported(GenericDevice device) {
            return false;
            // return device.Major == 5;
        }

        public void Handle(IRQCode code) {
            //
        }

        public void Add(PrimaryPty pty, SecondaryPty pts) {
            lock(_ptyLock) {
                Ptys.Add(
                    new PtyCtlEntry(pty, pts)
                );

                // Add pty to queue
            }
        }

        protected void StartKbdListener() {
            ITextIO reader = Kernel.Fs.Open(
                KbdEvent.FilePath,
                AccessMode.O_RDONLY
            );
            
            StartWorker(
                () => {
                    string key = reader.Read();

                    lock(_ptyLock) {
                        InputQueue.Enqueue(key);
                    }
                }
            );
        }

        protected void StartWorker(System.Action action) {
            Thread worker = new Thread(new ThreadStart(
                () => {
                    while (!Kernel.IsShutdown) {
                        action();
                    }
                }
            ));

            worker.Start();
        }

        protected void StartInputListener() {
            StartWorker(
                () => {
                    string key;

                    try {
                        lock(_ptyLock) {
                            key = InputQueue.Dequeue();
                        }

                        ProcessInput(key);
                    } catch (System.InvalidOperationException) {
                        //
                    }
                }
            );
        }

        void ProcessInput(string key) {
            Debug.Log("received key from driver: " + key);
        }
    }
}